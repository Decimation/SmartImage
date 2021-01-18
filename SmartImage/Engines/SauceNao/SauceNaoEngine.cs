using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Json;
using System.Linq;
using RestSharp;
using SimpleCore.Net;
using SimpleCore.Utilities;
using SmartImage.Core;
using SmartImage.Searching;
using JsonArray = System.Json.JsonArray;
using JsonObject = System.Json.JsonObject;

#nullable enable
// ReSharper disable InconsistentNaming
// ReSharper disable ParameterTypeCanBeEnumerable.Local
#pragma warning disable HAA0502, HAA0601, HAA0102, HAA0401
namespace SmartImage.Engines.SauceNao
{
	// https://github.com/RoxasShadow/SauceNao-Windows
	// https://github.com/LazDisco/SharpNao

	// NOTE: It seems that the SauceNao API works regardless of whether or not an API key is used

	/// <summary>
	///     SauceNao API client
	/// </summary>
	public sealed class SauceNaoEngine : BasicSearchEngine
	{
		private const string BASE_URL = "https://saucenao.com/";

		private const string BASIC_RESULT = "https://saucenao.com/search.php?url=";

		private const string ENDPOINT = BASE_URL + "search.php";


		private readonly string m_apiKey;

		private readonly RestClient m_client;

		private SauceNaoEngine(string apiKey) : base(BASIC_RESULT)
		{
			m_client = new RestClient(ENDPOINT);
			m_apiKey = apiKey;
		}

		public SauceNaoEngine() : this(SearchConfig.Config.SauceNaoAuth) { }

		public override string Name => "SauceNao";

		public override SearchEngineOptions Engine => SearchEngineOptions.SauceNao;

		public override Color Color => Color.OrangeRed;

		public override float? FilterThreshold => 70.00F;

		private BasicSearchResult[] ConvertResults(SauceNaoDataResult[] results)
		{
			var rg = new List<BasicSearchResult>();

			foreach (var sn in results) {
				if (sn.Urls != null) {
					string? url  = sn.Urls.FirstOrDefault(u => u != null)!;
					string? name = sn.Index.ToString();

					var x = new BasicSearchResult(url, sn.Similarity, 
						sn.WebsiteTitle, sn.Creator, sn.Material, sn.Character, name);

					x.Filter = x.Similarity < FilterThreshold;


					rg.Add(x);
				}
			}

			return rg.ToArray();
		}


		public override FullSearchResult GetResult(string url)
		{
			FullSearchResult result = base.GetResult(url);

			try {
				var orig = GetResults(url);

				if (orig == null) {
					return result;
				}

				// aggregate all info for primary result

				string? character = orig.FirstOrDefault(o => o.Character != null)?.Character;
				string? creator   = orig.FirstOrDefault(o => o.Creator   != null)?.Creator;
				string? material  = orig.FirstOrDefault(o => o.Material  != null)?.Material;


				var extended = ConvertResults(orig);

				var ordered = extended
					.Where(e => e.Url != null)
					.OrderByDescending(e => e.Similarity);

				var best = ordered.First();

				// Copy
				result.UpdateFrom(best);

				result.Characters = character;
				result.Artist     = creator;
				result.Source     = material;

				result.AddExtendedResults(extended);

				if (!String.IsNullOrWhiteSpace(m_apiKey)) {
					result.ExtendedInfo.Add("Using API");
				}

			}
			catch (Exception e) {
				Debug.WriteLine($"SauceNao error: {e.StackTrace}");
				result.ExtendedInfo.Add("Error parsing");
			}

			return result;
		}


		private SauceNaoDataResult[]? GetResults(string url)
		{
			var req = new RestRequest();
			req.AddQueryParameter("db", "999");
			req.AddQueryParameter("output_type", "2");
			req.AddQueryParameter("numres", "16");
			req.AddQueryParameter("api_key", m_apiKey);
			req.AddQueryParameter("url", url);

			var res = m_client.Execute(req);

			string c = res.Content;

			return ReadResults(c);
		}


		private static SauceNaoDataResult[]? ReadResults(string js)
		{
			// Excerpts of code adapted from https://github.com/Lazrius/SharpNao/blob/master/SharpNao.cs

			const string KeySimilarity = "similarity";
			const string KeyUrls       = "ext_urls";
			const string KeyIndex      = "index_id";
			const string KeyCreator    = "creator";
			const string KeyCharacters = "characters";
			const string KeyMaterial   = "material";

			const string KeyResults = "results";
			const string KeyHeader  = "header";
			const string KeyData    = "data";

			var jsonString = JsonValue.Parse(js);

			if (jsonString is JsonObject jsonObject) {

				var jsonArray = jsonObject[KeyResults];

				for (int i = 0; i < jsonArray.Count; i++) {

					var    header = jsonArray[i][KeyHeader];
					var    data   = jsonArray[i][KeyData];
					string obj    = header.ToString();
					obj          =  obj.Remove(obj.Length - 1);
					obj          += data.ToString().Remove(0, 1).Insert(0, ",");
					jsonArray[i] =  JsonValue.Parse(obj);
				}

				string json = jsonArray.ToString();

				var buffer      = new List<SauceNaoDataResult>();
				var resultArray = JsonValue.Parse(json);

				for (int i = 0; i < resultArray.Count; i++) {

					var   result     = resultArray[i];
					float similarity = Single.Parse(result[KeySimilarity]);

					string[]? strings = result.ContainsKey(KeyUrls)
						? (result[KeyUrls] as JsonArray)!.Select(j => j.ToString().CleanString()).ToArray()
						: null;

					var index = (SauceNaoSiteIndex) Int32.Parse(result[KeyIndex].ToString());

					var item = new SauceNaoDataResult
					{
						Urls       = strings,
						Similarity = similarity,
						Index      = index,
						Creator    = result.TryGetKeyValue(KeyCreator)?.ToString().CleanString(),
						Character  = result.TryGetKeyValue(KeyCharacters)?.ToString().CleanString(),
						Material   = result.TryGetKeyValue(KeyMaterial)?.ToString().CleanString()
					};


					buffer.Add(item);
				}

				return buffer.ToArray();

			}

			return null;
		}


		private class SauceNaoDataResult
		{
			/// <summary>
			///     The url(s) where the source is from. Multiple will be returned if the exact same image is found in multiple places
			/// </summary>
			public string[]? Urls { get; internal init; }

			/// <summary>
			///     The search index of the image
			/// </summary>
			public SauceNaoSiteIndex Index { get; internal init; }

			/// <summary>
			///     How similar is the image to the one provided (Percentage)?
			/// </summary>
			public float Similarity { get; internal init; }

			public string? WebsiteTitle { get; set; }

			public string? Character { get; internal init; }

			public string? Material { get; internal init; }

			public string? Creator { get; internal init; }

			public override string ToString()
			{
				string firstUrl = Urls != null ? Urls[0] : "-";

				return $"{firstUrl} ({Similarity}, {Index})";
			}
		}
	}
}