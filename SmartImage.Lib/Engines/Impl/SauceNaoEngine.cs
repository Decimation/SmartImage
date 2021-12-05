// ReSharper disable UnusedMember.Global

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Json;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Flurl;
using Flurl.Http;
using Kantan.Diagnostics;
using Kantan.Net;
using Kantan.Text;
using Kantan.Utilities;
using SmartImage.Lib.Engines.Model;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;
using static Kantan.Diagnostics.LogCategories;
using JsonArray = System.Json.JsonArray;
using JsonObject = System.Json.JsonObject;

// ReSharper disable PossibleNullReferenceException

// ReSharper disable PropertyCanBeMadeInitOnly.Local
// ReSharper disable StringLiteralTypo
// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable ParameterTypeCanBeEnumerable.Local

namespace SmartImage.Lib.Engines.Impl;

public sealed class SauceNaoEngine : ClientSearchEngine
{
	private const string BASE_URL = "https://saucenao.com/";

	private const string BASE_ENDPOINT = BASE_URL + "search.php";

	private const string BASIC_RESULT = $"{BASE_ENDPOINT}?url=";


	public override string Name => EngineOption.ToString();

	/*
	 * Excerpts adapted from https://github.com/Lazrius/SharpNao/blob/master/SharpNao.cs#L53
	 * https://github.com/luk1337/SauceNAO/blob/master/app/src/main/java/com/luk/saucenao/MainActivity.java
	 */

	public override EngineSearchType SearchType => EngineSearchType.Image | EngineSearchType.Metadata;

	public SauceNaoEngine(string authentication) : base(BASIC_RESULT, BASE_ENDPOINT)
	{
		Authentication = authentication;
	}

	public SauceNaoEngine() : this(null) { }

	public string Authentication { get; set; }

	public bool UsingAPI => !String.IsNullOrWhiteSpace(Authentication);

	public override SearchEngineOptions EngineOption => SearchEngineOptions.SauceNao;


	protected override SearchResult Process(object obj, SearchResult result)
	{
		var query = (ImageQuery) obj;

		var primaryResult = new ImageResult();

		var parseFunc = (Func<ImageQuery, Task<IEnumerable<SauceNaoDataResult>>>)
			(!UsingAPI ? GetWebResults : GetAPIResults);

		var now = Stopwatch.GetTimestamp();

		var dataResults = parseFunc(query);


		if (dataResults == null) {
			result.ErrorMessage = "Daily search limit (100) exceeded";
			result.Status       = ResultStatus.Cooldown;
			//return sresult;
			goto ret;
		}

		var imageResults = dataResults.Result.Where(o => o != null)
		                              .AsParallel()
		                              .Select(ConvertToImageResult)
		                              .Where(o => o != null)
		                              .OrderByDescending(e => e.Similarity)
		                              .ToList();


		if (!imageResults.Any()) {
			// No good results
			//return sresult;
			goto ret;
		}

		primaryResult.UpdateFrom(imageResults.First());

		primaryResult.Url ??= imageResults.FirstOrDefault(x => x.Url != null)?.Url;

		result.OtherResults.AddRange(imageResults);


		if (UsingAPI) {
			Debug.WriteLine($"{Name} API key: {Authentication}");
		}

		result.PrimaryResult = primaryResult;


		ret:

		result.PrimaryResult.Quality = result.PrimaryResult.Similarity switch
		{
			>= 75 => ResultQuality.High,
			_ or null    => ResultQuality.NA,
		};

		return result;
	}


	private async Task<IEnumerable<SauceNaoDataResult>> GetWebResults(ImageQuery query)
	{
		Trace.WriteLine($"{Name}: | Parsing HTML", LogCategories.C_INFO);

		var docp = new HtmlParser();

		var x = await EndpointUrl.PostMultipartAsync(m =>
		{
			m.AddString("url", query.IsUri ? query.Value : String.Empty);

			if (query.IsUri) { }
			else if (query.IsFile) {
				m.AddFile("file", query.Value, fileName: "image.png");
			}

			return;
		});
		var html = await x.GetStringAsync();


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


		static SauceNaoDataResult Parse(INode result)
		{
			if (result == null) {
				return null;
			}

			const string HIDDEN_ID_VAL = "result-hidden-notification";

			if (result.TryGetAttribute("id") == HIDDEN_ID_VAL) {
				return null;
			}

			var resulttablecontent = result.FirstChild
			                               .FirstChild
			                               .FirstChild
			                               .ChildNodes[1];

			var resultmatchinfo      = resulttablecontent.FirstChild;
			var resultsimilarityinfo = resultmatchinfo.FirstChild;

			// Contains links
			var resultmiscinfo      = resultmatchinfo.ChildNodes[1];
			var resultcontent       = resulttablecontent.ChildNodes[1];
			var resultcontentcolumn = resultcontent.ChildNodes[1];

			string link = null;

			var element = resultcontentcolumn.ChildNodes.GetElementsByTagName("a")
			                                 .FirstOrDefault(x => x.GetAttribute("href") != null);

			if (element != null) {
				link = element.GetAttribute("href");
			}

			//	//div[contains(@class, 'resulttitle')]
			//	//div/node()[self::strong]

			INode  resulttitle = resultcontent.ChildNodes[0];
			string rti         = resulttitle?.TextContent;

			INode  resultcontentcolumn1 = resultcontent.ChildNodes[1];
			string rcci                 = resultcontentcolumn1?.TextContent;

			string material1 = rcci?.SubstringAfter("Material: ");

			string creator1 = rti ?? rcci;
			creator1 = creator1.SubstringAfter("Creator: ");


			float similarity = Single.Parse(resultsimilarityinfo.TextContent.Replace("%", String.Empty));

			var dataResult = new SauceNaoDataResult
			{
				Urls       = new[] { link },
				Similarity = similarity,
				Creator    = creator1
			};

			return dataResult;

		}


		return results.Select(Parse).ToList();
	}

	private async Task<IEnumerable<SauceNaoDataResult>> GetAPIResults(ImageQuery url)
	{
		Trace.WriteLine($"{Name} | API");

		var client = new HttpClient();

		const string dbIndex = "999";
		const string numRes  = "6";

		var values = new Dictionary<string, string>
		{
			{ "db", dbIndex },
			{ "output_type", "2" },
			{ "api_key", Authentication },
			{ "url", url.ToString() },
			{ "numres", numRes }
		};

		var content = new FormUrlEncodedContent(values);

		var res = await client.PostAsync(BASE_ENDPOINT, content);
		var c   = await res.Content.ReadAsStringAsync();

		if (res.StatusCode == HttpStatusCode.Forbidden) {
			return null;
		}


		// Excerpts of code adapted from https://github.com/Lazrius/SharpNao/blob/master/SharpNao.cs

		const string KeySimilarity = "similarity";
		const string KeyUrls       = "ext_urls";
		const string KeyIndex      = "index_id";
		const string KeyCreator    = "creator";
		const string KeyCharacters = "characters";
		const string KeyMaterial   = "material";
		const string KeyResults    = "results";
		const string KeyHeader     = "header";
		const string KeyData       = "data";

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
					                   ? (result[KeyUrls] as JsonArray)!
					                     .Select(j => j.ToString().CleanString())
					                     .ToArray()
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

			return await Task.FromResult(buffer.ToArray());
		}

		return null;
	}

	private static ImageResult ConvertToImageResult(SauceNaoDataResult sn)
	{
		string url = sn.Urls?.FirstOrDefault(u => u != null);

		string siteName = sn.Index != 0 ? sn.Index.ToString() : null;

		var imageResult = new ImageResult
		{
			Url         = String.IsNullOrWhiteSpace(url) ? default : new Uri(url),
			Similarity  = MathF.Round(sn.Similarity, 2),
			Description = Strings.NormalizeNull(sn.WebsiteTitle),
			Artist      = Strings.NormalizeNull(sn.Creator),
			Source      = Strings.NormalizeNull(sn.Material),
			Characters  = Strings.NormalizeNull(sn.Character),
			Site        = Strings.NormalizeNull(siteName)
		};

		return imageResult;

	}

	/// <summary>
	/// Origin result
	/// </summary>
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
	}
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