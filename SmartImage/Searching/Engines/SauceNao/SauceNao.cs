using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Json;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using HtmlAgilityPack;
using RestSharp;
using SimpleCore.Utilities;
using SimpleCore.Win32.Cli;
using SmartImage.Searching.Model;
using SmartImage.Utilities;
using JsonObject = System.Json.JsonObject;

#nullable enable
// ReSharper disable InconsistentNaming
// ReSharper disable ParameterTypeCanBeEnumerable.Local

namespace SmartImage.Searching.Engines.SauceNao
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

		public ConsoleColor Color => ConsoleColor.White;


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

			Network.AssertResponse(res);


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

				var sr = new SearchResult(this, bestUrl, best.Similarity);
				sr.ExtendedInfo.Add("API configured");
				return sr;
			}

			return new SearchResult(this, null);
		}

		private readonly struct SauceNaoSimpleResult
		{
			public string Title { get; }
			public string Url { get; }
			public float Similarity { get; }

			public SauceNaoSimpleResult(string title, string url, float similarity)
			{
				Title = title;
				Url = url;
				Similarity = similarity;
			}

			public override string ToString()
			{
				return string.Format("{0} {1} {2}", Title, Url, Similarity);
			}
		}

		private static string FindCreator(HtmlNode resultcontent)
		{
			var resulttitle = resultcontent.ChildNodes[0];
			var rti = resulttitle?.InnerText;


			var resultcontentcolumn = resultcontent.ChildNodes[1];
			var rcci = resultcontentcolumn?.InnerText;

			var i = rti ?? rcci;

			return i;

		}
		
		// TODO: organize like Yandex


		private static List<SauceNaoSimpleResult> ParseResults(HtmlDocument doc)
		{
			var results = doc.DocumentNode.SelectNodes("//div[@class='result']");

			var images = new List<SauceNaoSimpleResult>();

			foreach (var result in results)
			{
				if (result.GetAttributeValue("id", string.Empty) == "result-hidden-notification")
				{
					continue;
				}

				var n = result.FirstChild.FirstChild;

				//var resulttableimage = n.ChildNodes[0];
				var resulttablecontent = n.ChildNodes[1];

				var resultmatchinfo = resulttablecontent.FirstChild;
				var resultsimilarityinfo = resultmatchinfo.FirstChild;

				// Contains links
				var resultmiscinfo = resultmatchinfo.ChildNodes[1];

				var links1 = resultmiscinfo.SelectNodes("a/@href");
				var link1 = links1?[0].GetAttributeValue("href", null);


				var resultcontent = resulttablecontent.ChildNodes[1];

				//var resulttitle = resultcontent.ChildNodes[0];

				var resultcontentcolumn = resultcontent.ChildNodes[1];

				// Other way of getting links
				var links2 = resultcontentcolumn.SelectNodes("a/@href");
				var link2 = links2?[0].GetAttributeValue("href", null);

				var link = link1 ?? link2;

				var title = FindCreator(resultcontent);
				var similarity = float.Parse(resultsimilarityinfo.InnerText.Replace("%", String.Empty));
				

				var i = new SauceNaoSimpleResult(title, link, similarity);
				images.Add(i);
			}

			return images;
		}

		private SearchResult GetBestResultWithoutApi(string url)
		{
			

			SearchResult? sr = null;

			var resUrl = BASIC_RESULT + url;


			var sz = Network.GetString(resUrl);
			var doc = new HtmlDocument();
			doc.LoadHtml(sz);

			try {
				
				var img = ParseResults(doc);

				var best = img.OrderByDescending(i=>i.Similarity).First(i => i.Url!=null);

				sr = new SearchResult(this, best.Url, best.Similarity);
				sr.ExtendedInfo.Add(best.Title);
			}
			catch (Exception e) {
				sr = new SearchResult(this, resUrl);
				sr.ExtendedInfo.Add("Error parsing");
			}


			return sr;


		}
	}
}