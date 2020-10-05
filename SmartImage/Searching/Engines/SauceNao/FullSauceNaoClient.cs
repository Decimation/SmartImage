using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Json;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using RestSharp;
using SimpleCore.Utilities;
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

	// NOTE: It seems that the SauceNao API works regardless of whether or not an API key is used

	/// <summary>
	/// SauceNao API client
	/// </summary>
	public sealed class FullSauceNaoClient : BaseSauceNaoClient
	{
		private const string ENDPOINT = BASE_URL + "search.php";


		private readonly string m_apiKey;

		private readonly RestClient m_client;

		private FullSauceNaoClient(string apiKey)
		{
			m_client = new RestClient(ENDPOINT);
			m_apiKey = apiKey;
		}

		public FullSauceNaoClient() : this(SearchConfig.Config.SauceNaoAuth) { }

		private static ISearchResult[] ConvertResults(SauceNaoResult[] results)
		{
			var rg = new ISearchResult[results.Length];

			for (int i = 0; i < rg.Length; i++) {
				var sn = results[i];

				rg[i] = new SauceNaoSimpleResult(sn.WebsiteTitle, sn.Url?.FirstOrDefault(u => u != null),
					sn.Similarity);
			}

			return rg;
		}

		public override SearchResult GetResult(string url)
		{
			SearchResult result=base.GetResult(url);

			try {
				var sn = GetResults(url)
					.OrderByDescending(r => r.Similarity)
					.ToArray();

				var extended = ConvertResults(sn);
				var best = extended[0];

				result.Url = best.Url;
				result.Similarity = best.Similarity;
				result.Caption = best.Caption;


				result.AddExtendedInfo(extended);
			}
			catch (Exception e) {
				
				result.ExtendedInfo.Add(e.StackTrace);
			}


			return result;
		}


		private SauceNaoResult[]? GetResults(string url)
		{

			var req = new RestRequest();
			req.AddQueryParameter("db", "999");
			req.AddQueryParameter("output_type", "2");
			req.AddQueryParameter("numres", "16");
			req.AddQueryParameter("api_key", m_apiKey);
			req.AddQueryParameter("url", url);


			var res = m_client.Execute(req);

			Network.AssertResponse(res);

			string c = res.Content;

			return ReadResults(c);

		}


		private static SauceNaoResult[]? ReadResults(string js)
		{

			// todo: rewrite this using Newtonsoft

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
	}
}