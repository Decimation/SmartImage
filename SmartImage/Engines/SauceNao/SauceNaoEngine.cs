using HtmlAgilityPack;
using RestSharp;
using SimpleCore.Net;
using SimpleCore.Utilities;
using SmartImage.Configuration;
using SmartImage.Searching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Json;
using System.Linq;
using System.Net;
using JsonArray = System.Json.JsonArray;
using JsonObject = System.Json.JsonObject;

// ReSharper disable PossibleMultipleEnumeration

#nullable enable

// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable ParameterTypeCanBeEnumerable.Local

namespace SmartImage.Engines.SauceNao
{
	// https://github.com/RoxasShadow/SauceNao-Windows
	// https://github.com/LazDisco/SharpNao


	/// <summary>
	///     SauceNao client
	/// </summary>
	public sealed class SauceNaoEngine : BaseSearchEngine
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

		#region HTML

		// todo
		// https://github.com/Decimation/SmartImage/blob/49b373305d4b9c96df393feabecf3c451a7c6a7d/SmartImage/Searching/Engines/SauceNao/AltSauceNaoClient.cs

		private static (string? Creator, string? Material) FindInfo(HtmlNode resultcontent)
		{
			var resulttitle = resultcontent.ChildNodes[0];
			string? rti = resulttitle?.InnerText;

			var resultcontentcolumn = resultcontent.ChildNodes[1];
			string? rcci = resultcontentcolumn?.InnerText;

			var material = rcci?.SubstringAfter("Material: ");

			// var resultcontentcolumn2 = resultcontent.ChildNodes[2];
			// var rcci2                = resultcontentcolumn2?.InnerText;


			// Debug.WriteLine($"[{rti}] [{rcci}] {material}");


			string? creator = rti ?? rcci;
			creator = creator?.SubstringAfter("Creator: ");

			return (creator, material);

		}


		private static IEnumerable<SauceNaoDataResult> ParseResults(string url)
		{
			var doc = new HtmlDocument();
			doc.LoadHtml(Network.GetString(BASIC_RESULT + url));

			// todo: improve

			var results = doc.DocumentNode.SelectNodes("//div[@class='result']");

			var images = new List<SauceNaoDataResult>();

			foreach (var result in results)
			{
				if (result.GetAttributeValue("id", String.Empty) == "result-hidden-notification")
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
				string? link1 = links1?[0].GetAttributeValue("href", null);


				var resultcontent = resulttablecontent.ChildNodes[1];

				//var resulttitle = resultcontent.ChildNodes[0];

				var resultcontentcolumn = resultcontent.ChildNodes[1];

				// Other way of getting links
				var links2 = resultcontentcolumn.SelectNodes("a/@href");
				string? link2 = links2?[0].GetAttributeValue("href", null);

				string? link = link1 ?? link2;

				var (creator, material) = FindInfo(resultcontent);
				float similarity = Single.Parse(resultsimilarityinfo.InnerText.Replace("%", String.Empty));


				var i = new SauceNaoDataResult
				{
					Urls = new[] { link }!,
					Similarity = similarity,
					Creator = creator,
				};

				images.Add(i);
			}

			return images;
		}

		#endregion

		#region API

		private BaseSearchResult[] ConvertDataResults(SauceNaoDataResult[] results)
		{
			var rg = new List<BaseSearchResult>();

			foreach (var sn in results)
			{
				if (sn.Urls != null)
				{
					string? url = sn.Urls.FirstOrDefault(u => u != null)!;

					string? siteName = sn.Index != 0 ? sn.Index.ToString() : null;

					// var x = new BasicSearchResult(url, sn.Similarity,
					// 	sn.WebsiteTitle, sn.Creator, sn.Material, sn.Character, siteName);
					var x = new BaseSearchResult()
					{
						Url = url,
						Similarity = sn.Similarity,
						Description = sn.WebsiteTitle,
						Artist = sn.Creator,
						Source = sn.Material,
						Characters = sn.Character,
						Site = siteName
					};
					x.Filter = x.Similarity < FilterThreshold;


					rg.Add(x);
				}
			}

			return rg.ToArray();
		}

		private static SauceNaoDataResult[]? ReadDataResults(string js)
		{
			// Excerpts of code adapted from https://github.com/Lazrius/SharpNao/blob/master/SharpNao.cs

			const string KeySimilarity = "similarity";
			const string KeyUrls = "ext_urls";
			const string KeyIndex = "index_id";
			const string KeyCreator = "creator";
			const string KeyCharacters = "characters";
			const string KeyMaterial = "material";

			const string KeyResults = "results";
			const string KeyHeader = "header";
			const string KeyData = "data";

			var jsonString = JsonValue.Parse(js);

			if (jsonString is JsonObject jsonObject)
			{

				var jsonArray = jsonObject[KeyResults];

				for (int i = 0; i < jsonArray.Count; i++)
				{

					var header = jsonArray[i][KeyHeader];
					var data = jsonArray[i][KeyData];
					string obj = header.ToString();
					obj = obj.Remove(obj.Length - 1);
					obj += data.ToString().Remove(0, 1).Insert(0, ",");
					jsonArray[i] = JsonValue.Parse(obj);
				}

				string json = jsonArray.ToString();

				var buffer = new List<SauceNaoDataResult>();
				var resultArray = JsonValue.Parse(json);

				for (int i = 0; i < resultArray.Count; i++)
				{

					var result = resultArray[i];
					float similarity = Single.Parse(result[KeySimilarity]);

					string[]? strings = result.ContainsKey(KeyUrls)
						? (result[KeyUrls] as JsonArray)!.Select(j => j.ToString().CleanString()).ToArray()
						: null;

					var index = (SauceNaoSiteIndex)Int32.Parse(result[KeyIndex].ToString());

					var item = new SauceNaoDataResult
					{
						Urls = strings,
						Similarity = similarity,
						Index = index,
						Creator = result.TryGetKeyValue(KeyCreator)?.ToString().CleanString(),
						Character = result.TryGetKeyValue(KeyCharacters)?.ToString().CleanString(),
						Material = result.TryGetKeyValue(KeyMaterial)?.ToString().CleanString()
					};


					buffer.Add(item);
				}

				return buffer.ToArray();

			}

			return null;
		}

		private SauceNaoDataResult[]? GetDataResults(string url)
		{
			var req = new RestRequest();
			req.AddQueryParameter("db", "999");
			req.AddQueryParameter("output_type", "2");
			req.AddQueryParameter("numres", "16");
			req.AddQueryParameter("api_key", m_apiKey);
			req.AddQueryParameter("url", url);

			var res = m_client.Execute(req);

			//Debug.WriteLine($"{res.StatusCode}");

			if (res.StatusCode == HttpStatusCode.Forbidden)
			{
				return null;
			}

			string c = res.Content;

			return ReadDataResults(c);
		}

		#endregion


		public override FullSearchResult GetResult(string url)
		{
			FullSearchResult result = base.GetResult(url);

			try
			{
				var orig = GetDataResults(url);

				if (orig == null)
				{
					//return result;
					Debug.WriteLine("Parsing HTML from SN!");
					orig = ParseResults(url).ToArray();
				}

				// aggregate all info for primary result

				string? character = orig.FirstOrDefault(o => !String.IsNullOrWhiteSpace(o.Character))?.Character;
				string? creator = orig.FirstOrDefault(o => !String.IsNullOrWhiteSpace(o.Creator))?.Creator;
				string? material = orig.FirstOrDefault(o => !String.IsNullOrWhiteSpace(o.Material))?.Material;


				var extended = ConvertDataResults(orig);

				var ordered = extended
					.Where(e => e.Url != null)
					.OrderByDescending(e => e.Similarity);


				if (!ordered.Any())
				{
					// No good results

					result.Filter = true;
					return result;
				}

				var best = ordered.First();


				// Copy
				result.UpdateFrom(best);

				result.Characters = character;
				result.Artist = creator;
				result.Source = material;

				result.AddExtendedResults(extended);

				if (!String.IsNullOrWhiteSpace(m_apiKey))
				{
					result.Metadata.Add("API", m_apiKey);
				}

			}
			catch (Exception e)
			{
				Debug.WriteLine($"SauceNao error: {e.StackTrace}");
				result.AddErrorMessage(e.Message);
			}

			return result;
		}


		private class SauceNaoDataResult
		{
			/// <summary>
			///     The url(s) where the source is from. Multiple will be returned if the exact same image is found in multiple places
			/// </summary>
			public string[]? Urls { get; internal set; }

			/// <summary>
			///     The search index of the image
			/// </summary>
			public SauceNaoSiteIndex Index { get; internal set; }

			/// <summary>
			///     How similar is the image to the one provided (Percentage)?
			/// </summary>
			public float Similarity { get; internal set; }

			public string? WebsiteTitle { get; internal set; }

			public string? Character { get; internal set; }

			public string? Material { get; internal set; }

			public string? Creator { get; internal set; }

			public override string ToString()
			{
				string firstUrl = Urls != null ? Urls[0] : "-";

				return $"{firstUrl} ({Similarity}, {Index}) {Creator}";
			}
		}
	}
}