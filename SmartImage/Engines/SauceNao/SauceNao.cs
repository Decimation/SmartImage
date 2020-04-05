using System;
using System.IO;
using System.Json;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using RestSharp;
using SmartImage.Model;
using SmartImage.Utilities;
using JsonObject = System.Json.JsonObject;
// ReSharper disable InconsistentNaming
// ReSharper disable ParameterTypeCanBeEnumerable.Local

namespace SmartImage.Engines.SauceNao
{
	// https://github.com/RoxasShadow/SauceNao-Windows
	// https://github.com/LazDisco/SharpNao

	public sealed class SauceNao : ISearchEngine
	{
		private const string ENDPOINT = "https://saucenao.com/search.php";

		private readonly RestClient m_client;

		private readonly string m_apiKey;

		private const string NAME = "SauceNao";

		private SauceNao(string apiKey)
		{
			m_client = new RestClient(ENDPOINT);
			m_apiKey = apiKey;
		}

		public SauceNao() : this(Config.SauceNaoAuth) { }

		public SauceNaoResult[] GetSNResults(string url)
		{
			var req = new RestRequest();
			req.AddQueryParameter("db", "999");
			req.AddQueryParameter("output_type", "2");
			req.AddQueryParameter("numres", "16");
			req.AddQueryParameter("api_key", m_apiKey);
			req.AddQueryParameter("url", url);


			var res = m_client.Execute(req);
			
			Common.AssertResponse(res);


			//Console.WriteLine("{0} {1} {2}", res.IsSuccessful, res.ResponseStatus, res.StatusCode);
			//Console.WriteLine(res.Content);


			var c = res.Content;


			if (string.IsNullOrWhiteSpace(c)) {
				Cli.WriteError("No SN results!");
			}

			return ReadResults(c);
		}

		private SauceNaoResult[] ReadResults(string c)
		{
			// From https://github.com/Lazrius/SharpNao/blob/master/SharpNao.cs
			
			var jsonString = JsonValue.Parse(c);
			
			if (jsonString is JsonObject jsonObject) {
				var jsonArray = jsonObject["results"];
				for (int i = 0; i < jsonArray.Count; i++) {
					var    header = jsonArray[i]["header"];
					var    data   = jsonArray[i]["data"];
					string obj    = header.ToString();
					obj          =  obj.Remove(obj.Length - 1);
					obj          += data.ToString().Remove(0, 1).Insert(0, ",");
					jsonArray[i] =  JsonValue.Parse(obj);
				}

				string json = jsonArray.ToString();
				json = json.Insert(json.Length - 1, "}").Insert(0, "{\"results\":");
				using var stream =
					JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(json),
					                                         XmlDictionaryReaderQuotas.Max);
				var serializer = new DataContractJsonSerializer(typeof(SauceNaoResponse));
				var result     = serializer.ReadObject(stream) as SauceNaoResponse;
				stream.Dispose();
				if (result is null)
					return null;

				foreach (var t in result.Results) {
					t.WebsiteTitle = Common.SplitPascalCase(t.Index.ToString());
				}

				return result.Results;
			}

			return null;
		}

		public SearchEngines Engine => SearchEngines.SauceNao;

		private static SauceNaoResult GetBestSNResult(SauceNaoResult[] results)
		{
			var sauceNao = results.OrderByDescending(r => r.Similarity).First();

			return sauceNao;
		}

		public string GetRawResult(string url)
		{
			var res = GetSNResults(url);
			return GetBestSNResult(res).Url[0];
		}

		public SearchResult GetResult(string url)
		{
			var sn = GetSNResults(url);

			if (sn == null) {
				return new SearchResult(null, NAME);
			}

			var best = GetBestSNResult(sn);

			
			if (best != null) {
				string bestUrl = best?.Url?[0];

				var sr = new SearchResult(bestUrl, NAME, best.Similarity)
				{
					ExtendedInfo = new[]
					{
						String.Format("Similarity: {0:P}", best.Similarity / 100)
					}
				};


				return sr;
			}

			return new SearchResult(null, NAME);
		}
	}
}