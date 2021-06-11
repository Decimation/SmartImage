// ReSharper disable UnusedMember.Global

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Json;
using System.Linq;
using System.Net;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using RestSharp;
using SimpleCore.Diagnostics;
using SimpleCore.Net;
using SimpleCore.Utilities;
using SmartImage.Lib.Searching;
using static SimpleCore.Diagnostics.LogCategories;
using JsonArray = System.Json.JsonArray;
using JsonObject = System.Json.JsonObject;
// ReSharper disable PropertyCanBeMadeInitOnly.Local

// ReSharper disable PossibleMultipleEnumeration

// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable ParameterTypeCanBeEnumerable.Local
namespace SmartImage.Lib.Engines.Impl
{
	public sealed class SauceNaoEngine : BaseSearchEngine
	{
		// Excerpts adapted from https://github.com/Lazrius/SharpNao/blob/master/SharpNao.cs#L53

		private const string BASE_URL = "https://saucenao.com/";

		private const string BASIC_RESULT = "https://saucenao.com/search.php?url=";

		private const string ENDPOINT = BASE_URL + "search.php";

		private readonly RestClient m_client;

		// todo

		public SauceNaoEngine(string authentication) : base(BASIC_RESULT)
		{
			m_client       = new RestClient(ENDPOINT);
			Authentication = authentication;
		}

		public SauceNaoEngine() : this(String.Empty) { } //todo

		public string Authentication { get; init; }

		public override SearchEngineOptions EngineOption => SearchEngineOptions.SauceNao;


		private static IEnumerable<SauceNaoDataResult> ParseResults(string url)
		{
			var docp = new HtmlParser();

			var rc  = new RestClient(BASE_URL);
			var req = new RestRequest("search.php");
			req.AddQueryParameter("url", url);

			var execute = rc.Execute(req);

			string html = execute.Content;

			/*
			 * Daily Search Limit Exceeded.
			 * 208.110.232.218, your IP has exceeded the unregistered user's daily limit of 100 searches.
			 */

			const string COOLDOWN = "Search Limit Exceeded";

			if (html.Contains(COOLDOWN)) {
				Trace.WriteLine("SauceNao on cooldown!", C_WARN);

				return null;
			}

			var doc = docp.ParseDocument(html);

			const string RESULT_NODE = "//div[@class='result']";

			var results = doc.Body.SelectNodes(RESULT_NODE);

			return results.Select(Parse).ToList();
		}

		private static SauceNaoDataResult Parse(INode result)
		{
			if (result == null) {
				return null;
			}

			const string HIDDEN_ID_VAL = "result-hidden-notification";

			if (result.TryGetAttribute("id") == HIDDEN_ID_VAL) {
				return null;
			}

			var resulttablecontent = result.FirstChild.FirstChild.FirstChild.ChildNodes[1];

			var resultmatchinfo      = resulttablecontent.FirstChild;
			var resultsimilarityinfo = resultmatchinfo.FirstChild;

			// Contains links
			var resultmiscinfo = resultmatchinfo.ChildNodes[1];

			var resultcontent = resulttablecontent.ChildNodes[1];

			var resultcontentcolumn = resultcontent.ChildNodes[1];

			string link = null;

			var g = resultcontentcolumn.ChildNodes.GetElementsByTagName("a")
			                           .FirstOrDefault(x => x.GetAttribute("href") != null);

			if (g != null) {
				link = g.GetAttribute("href");
			}

			//	//div[contains(@class, 'resulttitle')]
			//	//div/node()[self::strong]

			var    resulttitle = resultcontent.ChildNodes[0];
			string rti         = resulttitle?.TextContent;

			var    resultcontentcolumn1 = resultcontent.ChildNodes[1];
			string rcci                 = resultcontentcolumn1?.TextContent;

			string material1 = rcci?.SubstringAfter("Material: ");

			string creator1 = rti ?? rcci;
			creator1 = creator1?.SubstringAfter("Creator: ");


			float similarity = Single.Parse(resultsimilarityinfo.TextContent.Replace("%", String.Empty));

			var dataResult = new SauceNaoDataResult
			{
				Urls       = new[] {link}!,
				Similarity = similarity,
				Creator    = creator1
			};

			return dataResult;
		}

		public override SearchResult GetResult(ImageQuery url)
		{
			var sresult = base.GetResult(url);

			var result = new ImageResult();

			try {
				var orig = GetDataResults(url.Uri.ToString());

				if (orig == null) {
					//return result;
					Debug.WriteLine($"[{Name}] Parsing HTML", LogCategories.C_INFO);

					string urlStr = url.Uri.ToString();

					var sauceNaoDataResults = ParseResults(urlStr);

					if (sauceNaoDataResults == null) {
						sresult.ErrorMessage = "Daily search limit (100) exceeded";
						sresult.Status       = ResultStatus.Unavailable;
						return sresult;
					}

					orig = sauceNaoDataResults.Where(o => o != null).ToArray();
				}

				// aggregate all info for primary result

				string character = orig.FirstOrDefault(o => !String.IsNullOrWhiteSpace(o.Character))?.Character;
				string creator   = orig.FirstOrDefault(o => !String.IsNullOrWhiteSpace(o.Creator))?.Creator;
				string material  = orig.FirstOrDefault(o => !String.IsNullOrWhiteSpace(o.Material))?.Material;

				var extended = orig.AsParallel().Select(ConvertToImageResult);

				var ordered = extended
				              .Where(e => e.Url != null)
				              .OrderByDescending(e => e.Similarity)
				              .ToList();

				if (!ordered.Any()) {
					// No good results
					//Debug.WriteLine($"No good results");
					return sresult;
				}

				var best = ordered.First();

				// Copy
				result.UpdateFrom(best);

				result.Characters = character;
				result.Artist     = creator;
				result.Source     = material;

				sresult.OtherResults.AddRange(extended);

				if (!String.IsNullOrWhiteSpace(Authentication)) {
					Debug.WriteLine($"{Name} API key: {Authentication}");
				}
			}
			catch (Exception e) {
				Debug.WriteLine($"{Name} error: {e.StackTrace}", C_ERROR);
				sresult.Status = ResultStatus.Failure;
			}

			sresult.PrimaryResult = result;

			return sresult;
		}

		private class SauceNaoDataResult
		{
			/// <summary>
			///     The url(s) where the source is from. Multiple will be returned if the exact same image is found in multiple places
			/// </summary>
			public string[] Urls { get; internal set; }

			/// <summary>
			///     The search index of the image
			/// </summary>
			public SauceNaoSiteIndex Index { get; internal set; }

			/// <summary>
			///     How similar is the image to the one provided (Percentage)?
			/// </summary>
			public float Similarity { get; internal set; }

			public string WebsiteTitle { get; internal set; }

			public string Character { get; internal set; }

			public string Material { get; internal set; }

			public string Creator { get; internal set; }

			public override string ToString()
			{
				string firstUrl = Urls != null ? Urls[0] : "-";

				return $"{firstUrl} ({Similarity}, {Index}) {Creator}";
			}
		}

		#region API

		private static ImageResult ConvertToImageResult(SauceNaoDataResult sn)
		{
			if (sn.Urls != null) {
				string url = sn.Urls.FirstOrDefault(u => u != null)!;

				string siteName = sn.Index != 0 ? sn.Index.ToString() : null;

				// var x = new BasicSearchResult(url, sn.Similarity,
				// 	sn.WebsiteTitle, sn.Creator, sn.Material, sn.Character, siteName);
				var imageResult = new ImageResult
				{
					Url         = String.IsNullOrWhiteSpace(url) ? default : new Uri(url),
					Similarity  = MathF.Round(sn.Similarity, 2),
					Description = sn.WebsiteTitle,
					Artist      = sn.Creator,
					Source      = sn.Material,
					Characters  = sn.Character,
					Site        = siteName
				};

				return imageResult;
			}

			return null;
		}

		private SauceNaoDataResult[] GetDataResults(string url)
		{
			var req = new RestRequest();
			req.AddQueryParameter("db", "999");
			req.AddQueryParameter("output_type", "2");
			req.AddQueryParameter("numres", "16");
			req.AddQueryParameter("api_key", Authentication);
			req.AddQueryParameter("url", url);

			var res = m_client.Execute(req);

			//Debug.WriteLine($"{res.StatusCode}");

			if (res.StatusCode == HttpStatusCode.Forbidden) {
				return null;
			}

			string c = res.Content;

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

			var jsonString = JsonValue.Parse(c);

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

					string[] strings = result.ContainsKey(KeyUrls)
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

		#endregion API
	}

	public enum SauceNaoSiteIndex
	{
		DoujinshiMangaLexicon = 3,
		Pixiv                 = 5,
		PixivArchive          = 6,
		NicoNicoSeiga         = 8,
		Danbooru              = 9,
		Drawr                 = 10,
		Nijie                 = 11,
		Yandere               = 12,
		OpeningsMoe           = 13,
		FAKKU                 = 16,
		nHentai               = 18,
		TwoDMarket            = 19,
		MediBang              = 20,
		AniDb                 = 21,
		IMDB                  = 23,
		Gelbooru              = 25,
		Konachan              = 26,
		SankakuChannel        = 27,
		AnimePictures         = 28,
		e621                  = 29,
		IdolComplex           = 30,
		BcyNetIllust          = 31,
		BcyNetCosplay         = 32,
		PortalGraphics        = 33,
		DeviantArt            = 34,
		Pawoo                 = 35,
		MangaUpdates          = 36,

		//
		ArtStation = 39,

		FurAffinity = 40,
		Twitter     = 41
	}
}