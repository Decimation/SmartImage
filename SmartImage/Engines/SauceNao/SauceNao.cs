using System;
using System.Diagnostics;
using System.Json;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using HtmlAgilityPack;
using RestSharp;
using SimpleCore.Utilities;
using SmartImage.Searching;
using SmartImage.Utilities;
using JsonObject = System.Json.JsonObject;

#nullable enable
// ReSharper disable InconsistentNaming
// ReSharper disable ParameterTypeCanBeEnumerable.Local

namespace SmartImage.Engines.SauceNao
{
	// https://github.com/RoxasShadow/SauceNao-Windows
	// https://github.com/LazDisco/SharpNao

	public sealed class SauceNao : ISearchEngine
	{
		private const string ENDPOINT = BASE_URL + "search.php";

		private const string BASE_URL = "https://saucenao.com/";

		private const string BASIC_RESULT = "https://saucenao.com/search.php?url=";

		private readonly string? m_apiKey;

		private readonly RestClient m_client;

		private readonly bool m_useApi;

		private SauceNao(string? apiKey)
		{
			m_client = new RestClient(ENDPOINT);
			m_apiKey = apiKey;
			m_useApi = !String.IsNullOrWhiteSpace(m_apiKey);
		}

		public SauceNao() : this(SearchConfig.Config.SauceNaoAuth) { }

		public string Name => "SauceNao";

		public SearchEngines Engine => SearchEngines.SauceNao;

		public SearchResult GetResult(string url)
		{
			return m_useApi ? GetBestResultWithApi(url) : GetBestResultWithoutApi(url);
		}

		public ConsoleColor Color => ConsoleColor.Gray;


		private SauceNaoResult[] GetApiResults(string url)
		{
			Debug.Assert(m_useApi);


			var req = new RestRequest();
			req.AddQueryParameter("db", "999");
			req.AddQueryParameter("output_type", "2");
			req.AddQueryParameter("numres", "16");
			req.AddQueryParameter("api_key", m_apiKey);
			req.AddQueryParameter("url", url);


			var res = m_client.Execute(req);

			NetworkUtilities.AssertResponse(res);


			//Console.WriteLine("{0} {1} {2}", res.IsSuccessful, res.ResponseStatus, res.StatusCode);
			//Console.WriteLine(res.Content);


			string c = res.Content;


			if (String.IsNullOrWhiteSpace(c)) {
				CliOutput.WriteError("No SN results!");
			}

			return ReadResults(c);
		}

		private static SauceNaoResult[] ReadResults(string js)
		{
			// From https://github.com/Lazrius/SharpNao/blob/master/SharpNao.cs

			var jsonString = JsonValue.Parse(js);

			if (jsonString is JsonObject jsonObject) {
				var jsonArray = jsonObject["results"];

				for (int i = 0; i < jsonArray.Count; i++) {
					var header = jsonArray[i]["header"];
					var data = jsonArray[i]["data"];
					string obj = header.ToString();
					obj = obj.Remove(obj.Length - 1);
					obj += data.ToString().Remove(0, 1).Insert(0, ",");
					jsonArray[i] = JsonValue.Parse(obj);
				}

				string json = jsonArray.ToString();
				json = json.Insert(json.Length - 1, "}").Insert(0, "{\"results\":");

				using var stream =
					JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(json),
						XmlDictionaryReaderQuotas.Max);
				var serializer = new DataContractJsonSerializer(typeof(SauceNaoResponse));
				var result = serializer.ReadObject(stream) as SauceNaoResponse;
				stream.Dispose();

				if (result is null)
					return null;

				foreach (var t in result.Results) {
					t.WebsiteTitle = Strings.SplitPascalCase(t.Index.ToString());
				}

				return result.Results;
			}

			return null;
		}


		private SearchResult GetBestResultWithApi(string url)
		{
			SauceNaoResult[] sn = GetApiResults(url);

			if (sn == null) {
				return new SearchResult(this, null);
			}

			var best = sn.OrderByDescending(r => r.Similarity).First();

			if (best != null) {
				string? bestUrl = best?.Url?[0];

				var sr = new SearchResult(this, bestUrl, best.Similarity / 100);
				sr.ExtendedInfo.Add("API configured");
				return sr;
			}

			return new SearchResult(this, null);
		}


		private SearchResult GetBestResultWithoutApi(string url)
		{
			/*string u  = BASIC_RESULT + url;
			var    sr = new SearchResult(u, Name);
			sr.ExtendedInfo.Add("API not configured");
			return sr;*/

			SearchResult? sr = null;

			var sz = NetworkUtilities.GetString(BASIC_RESULT + url);
			var doc = new HtmlDocument();
			doc.LoadHtml(sz);


			// todo: for now, just return the first link found, as SN already sorts by similarity and the first link is the best result
			var links = doc.DocumentNode.SelectNodes("//*[@class='resultcontentcolumn']/a/@href");

			foreach (var link in links) {
				var lk = link.GetAttributeValue("href", null);

				if (lk != null) {
					sr = new SearchResult(this, lk);
					break;
				}
			}

			if (sr == null) {
				string u = BASIC_RESULT + url;
				sr = new SearchResult(this, u);
			}

			return sr;


			// TODO: finish for more refined and complete results

			/*var classToGet = "result";

			var results = doc.DocumentNode.SelectNodes("//*[@class='" + classToGet + "']");

			Console.WriteLine("nresults: "+results.Count);

			foreach (HtmlNode node in results)
			{
				//string value = node.InnerText;
				// etc...

				//var n2 = node.SelectSingleNode("//*[@id=\"middle\"]/div[3]/table/tbody/tr/td[2]/div[1]/div[1]");

				//Console.WriteLine(n2.InnerText);

				// //*[@id="middle"]/div[2]/table/tbody/tr/td[2]/div[1]
				// //*[@id="middle"]/div[3]/table/tbody/tr/td[2]/div[1] 
				// //*[@id="middle"]/div[3]/table/tbody/tr/td[2]/div[1]

				var table = node.FirstChild;
				var tbody = table.FirstChild;
				var resulttablecontent = tbody.ChildNodes[1];
				var resultmatchinfo = resulttablecontent.FirstChild;
				
				var resultsimilarityinfo = resultmatchinfo.FirstChild;

				var resultcontent = resulttablecontent.ChildNodes[1];
				var resulttitle = resultcontent.FirstChild;

				// resultcontentcolumn comes after resulttitle

				// //*[@class='resultcontentcolumn']/a

				/*var links = resultcontent.SelectNodes("//*[@class='resultcontentcolumn']/a/@href");
				var links2 = links.Where(l => l.Name != "span").ToArray();
				
				foreach (var link in links2) {
					string hrefValue = link.GetAttributeValue("href", string.Empty);
					Console.WriteLine(">>> "+hrefValue);
				}#1#

				foreach (var resultcontentChildNode in resultcontent.ChildNodes) {
					if (resultcontentChildNode.GetAttributeValue("class", String.Empty)=="resultcontentcolumn") {
						foreach (var rcccnChildNode in resultcontentChildNode.ChildNodes) {
							var href = rcccnChildNode.GetAttributeValue("href", String.Empty);

							if (!string.IsNullOrWhiteSpace(href)) {
								Console.WriteLine("!>>>>"+href);
							}
						}
					}
				}

				Console.WriteLine(resultsimilarityinfo.InnerText);
			}*/

			//Console.ReadLine();
			//var sr = new SearchResult(BASIC_RESULT+url,Name);

			//return sr;


			/*var x = doc.DocumentNode.SelectNodes("//*[@class='resulttable']");

			foreach (var node in x)
			{
				var tbody = node.FirstChild;
				var resulttableimage = tbody.ChildNodes[0];
				var resulttablecontent = tbody.ChildNodes[1];
				var resultmatchinfo = resulttablecontent.FirstChild;
				var resultsimilarityinfo = resultmatchinfo.FirstChild;



				var resultcontent = resulttablecontent.ChildNodes[1];
				var resulttitle = resultcontent.FirstChild;
				var resultcontentcolumn = resultcontent.ChildNodes[1];
				var link = resultcontentcolumn.ChildNodes.First(n => n.GetAttributeValue("href", null) != null);
				var lk = link.GetAttributeValue("href", null);
				Console.WriteLine(">> {0} {1}", resultsimilarityinfo.InnerText, lk);
			}

			return null;*/

		}
	}
}