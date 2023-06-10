// ReSharper disable UnusedMember.Global

using System.Diagnostics;
using System.Json;
using System.Net;
using System.Text;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Flurl.Http;
using Kantan.Net.Utilities;
using Kantan.Text;
using SmartImage.Lib.Results;
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

namespace SmartImage.Lib.Engines.Impl.Search;

public sealed class SauceNaoEngine : BaseSearchEngine, IClientSearchEngine
{
	private const string BASE_URL = "https://saucenao.com/";

	private const string BASE_ENDPOINT = BASE_URL + "search.php";

	private const string BASIC_RESULT = $"{BASE_ENDPOINT}?url=";

	/*
	 * Excerpts adapted from https://github.com/Lazrius/SharpNao/blob/master/SharpNao.cs#L53
	 * https://github.com/luk1337/SauceNAO/blob/master/app/src/main/java/com/luk/saucenao/MainActivity.java
	 */

	public string EndpointUrl => BASE_ENDPOINT;

	public SauceNaoEngine(string authentication) : base(BASIC_RESULT)
	{
		Authentication = authentication;

	}

	public SauceNaoEngine() : this(null) { }

	public string Authentication { get; set; }

	public bool UsingAPI => !string.IsNullOrWhiteSpace(Authentication);

	public override SearchEngineOptions EngineOption => SearchEngineOptions.SauceNao;

	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken? token = null)
	{
		var result = await base.GetResultAsync(query, token);

		IEnumerable<SauceNaoDataResult> dataResults;

		try {
			if (UsingAPI) {
				dataResults = await GetAPIResultsAsync(query);
				Debug.WriteLine($"{Name} API key: {Authentication}");
			}
			else {
				dataResults = await GetWebResultsAsync(query);
			}
		}
		catch (Exception e) {
			result.ErrorMessage = e.Message;
			result.Status       = SearchResultStatus.Failure;
			return result;
		}

		if (!dataResults.Any()) {
			result.ErrorMessage = "Daily search limit (50) exceeded";
			result.Status       = SearchResultStatus.Cooldown;
			//return sresult;
			goto ret;
		}

		var imageResults = dataResults.Where(o => o != null)
			// .AsParallel()
			.Select((x) => ConvertToImageResult(x, result))
			.Where(o => o != null)
			.OrderByDescending(e => e.Similarity)
			.ToList();

		if (!imageResults.Any()) {
			// No good results
			//return sresult;
			result.Status = SearchResultStatus.NoResults;
			goto ret;
		}

		result.Results.AddRange(imageResults);

		// result.Results[0] = (imageResults.First());

		// result.Url ??= imageResults.FirstOrDefault(x => x.Url != null)?.Url;

		ret:

		result.Update();

		return result;
	}

	public override void Dispose() { }

	private async Task<IEnumerable<SauceNaoDataResult>> GetWebResultsAsync(SearchQuery query)
	{
		Trace.WriteLine($"{Name}: | Parsing HTML", C_INFO);

		var docp = new HtmlParser();

		string         html     = null;
		IFlurlResponse response = null;

		try {
			response = await EndpointUrl.AllowHttpStatus()
				           .PostMultipartAsync(m =>
				           {
					           m.AddString("url", query.Uni.IsUri ? query.Uni.Value.ToString() : string.Empty);

					           if (query.Uni.IsUri) { }
					           else if (query.Uni.IsFile) {
						           m.AddFile("file", query.Uni.Value.ToString(), fileName: "image.png");
					           }

					           return;
				           });
			html = await response.GetStringAsync();
		}
		catch (FlurlHttpException e) {

			/*
			 * Daily Search Limit Exceeded.
			 * <IP>, your IP has exceeded the unregistered user's daily limit of 100 searches.
			 */

			if (e.StatusCode == (int) HttpStatusCode.TooManyRequests) {
				Trace.WriteLine($"On cooldown!", Name);
				return await Task.FromResult(Enumerable.Empty<SauceNaoDataResult>());
			}

			html = await e.GetResponseStringAsync();
		}

		/*var raw=await GetRawUrlAsync(query);
		var html2=await raw.GetStringAsync();*/

		var doc = await docp.ParseDocumentAsync(html);

		var results = doc.Body.SelectNodes("//div[@class='result']");

		static SauceNaoDataResult Parse(INode result)
		{
			if (result == null) {
				return null;
			}

			const string HIDDEN_ID_VAL = "result-hidden-notification";

			if (result.TryGetAttribute(Serialization.Atr_id) == HIDDEN_ID_VAL) {
				return null;
			}

			var resulttablecontent = result.FirstChild
				.FirstChild
				.FirstChild
				.ChildNodes[1];

			var resultmatchinfo      = resulttablecontent.FirstChild;
			var resultsimilarityinfo = resultmatchinfo.FirstChild;

			// Contains links
			var resultmiscinfo = resultmatchinfo.ChildNodes[1];
			var resultcontent  = resulttablecontent.ChildNodes[1];
			// var resultcontentcolumn = resultcontent.ChildNodes[1];

			IHtmlCollection<IElement> resultcontentcolumn_rg = null;

			if (result is IElement { } elem) {
				resultcontentcolumn_rg = elem.QuerySelectorAll(Serialization.S_SauceNao_ResultContentColumn);

			}
			// var resulttitle = resultcontent.ChildNodes[0];

			var links = new List<string>();

			if (resulttablecontent is IElement { } e) {
				var links1 = e.QuerySelectorAll("a").Select(x => x.GetAttribute(Serialization.Atr_href));
				links.AddRange(links1);
			}

			var element = resultcontentcolumn_rg.Select(c => c.ChildNodes)
				.SelectMany(c => c.GetElementsByTagName(Serialization.Tag_a)
					            .Select(x => x.GetAttribute(Serialization.Atr_href)))
				.Where(e => e != null);

			if (element.Any()) {
				links.AddRange(element);
			}

			if (resultmiscinfo != null) {
				links.Add(resultmiscinfo.ChildNodes.GetElementsByTagName(Serialization.Tag_a)
					          .FirstOrDefault(x => x.GetAttribute(Serialization.Atr_href) != null)?
					          .GetAttribute(Serialization.Atr_href));
			}

			//	//div[contains(@class, 'resulttitle')]
			//	//div/node()[self::strong]

			INode  resulttitle = resultcontent.ChildNodes[0];
			string rti         = resulttitle?.TextContent;

			// INode  resultcontentcolumn1 = resultcontent.ChildNodes[1];
			string rcci = resultcontentcolumn_rg.FuncJoin(e => e.TextContent, ",");

			var synonyms   = new[] { "Creator(s):", "Creator:", "Member:", "Artist:", "Author:" };
			var material   = new[] { "Material:", "Source:" };
			var characters = new[] { "Characters:" };

			// string material1 = rcci.SubstringAfter(material);
			string material1 = rcci.SubstringAfter(material.First());

			// string creator1 = rcci;
			string creator1    = rcci;
			string characters1 = null;

			foreach (var s in synonyms) {
				if (rti.StartsWith(s)) {
					rti = rti.SubstringAfter(s).Trim(' ');
				}
			}
			// creator1 = creator1.SubstringAfter("Creator: ");
			// resultcontentcolumn.GetNodes(true, (IElement element) => element.LocalName == "strong");

			// resultcontentcolumn.GetNodes(deep:true, predicate: (INode n)=>n.TryGetAttribute() )
			// var t = resultcontentcolumn.ChildNodes[0].TextContent;

			var nodes = resultcontentcolumn_rg.SelectMany(e => e.ChildNodes)
				.Where(c => c is not (IElement { TagName: "BR" }
					            or IElement { NodeName: "SPAN" }))
				.ToArray();

			for (int i = 0; i < nodes.Length - 1; i += 2) {
				var n  = nodes[i];
				var n2 = nodes[i + 1];

				var nStr  = n.TextContent;
				var n2Str = n2.TextContent;

				if (synonyms.Any(s => nStr.StartsWith(s))) {
					creator1 = n2Str;
				}

				if (material.Any(s => nStr.StartsWith(s))) {
					material1 = n2Str;
				}

				if (characters.Any(s => nStr.StartsWith(s))) {
					characters1 = n2Str;
				}
			}

			/*if (resultcontentcolumn.ChildNodes.Length >= 2) {
				string creatorTitle = null;

				creatorTitle +=
					$"{resultcontentcolumn.ChildNodes[0].TextContent} {resultcontentcolumn.ChildNodes[1].TextContent}\n";

				if (resultcontentcolumn.ChildNodes.Length >= 6) {
					creatorTitle +=
						$"{resultcontentcolumn.ChildNodes[4].TextContent} {resultcontentcolumn.ChildNodes[5].TextContent}";

				}

				creator1 = creatorTitle;
			}*/

			// resultcontentcolumn.ChildNodes[1].TryGetAttribute("href");

			/*for (int i = 0; i < resultcontentcolumn.ChildNodes.Length - 1; i++) {
				if (i % 3 == 0 && i != 0) {
					continue;
				}

				var cn1 = resultcontentcolumn.ChildNodes[i];
				var cn2 = resultcontentcolumn.ChildNodes[i + 1];
				title1 += $"{cn1.TextContent} {cn2.TextContent}\n";
			}*/

			float similarity = float.Parse(resultsimilarityinfo.TextContent.Replace("%", string.Empty));

			var dataResult = new SauceNaoDataResult
			{
				Urls       = links.Distinct().ToArray(),
				Similarity = similarity,
				Creator    = creator1,
				Title      = rti,
				Material   = material1

			};

			return dataResult;

		}

		return results.Select(Parse).ToList();
	}

	private async Task<IEnumerable<SauceNaoDataResult>> GetAPIResultsAsync(SearchQuery url)
	{
		Trace.WriteLine($"{Name} | API");

		// var client = new HttpClient();

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

		var res = await BASE_ENDPOINT.AllowAnyHttpStatus()
			          .PostAsync(content);
		var c = await res.GetStringAsync();

		if (res.ResponseMessage.StatusCode == HttpStatusCode.Forbidden) {
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
				float similarity = float.Parse(result[KeySimilarity]);

				string[] strings = result.ContainsKey(KeyUrls)
					                   ? (result[KeyUrls] as JsonArray)!
					                   .Select(j => j.ToString().CleanString())
					                   .ToArray()
					                   : null;

				var index = (SauceNaoSiteIndex) int.Parse(result[KeyIndex].ToString());

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

	private static SearchResultItem ConvertToImageResult(SauceNaoDataResult sn, SearchResult r)
	{
		string siteName = sn.Index != 0 ? sn.Index.ToString() : null;

		var site  = Strings.NormalizeNull(siteName);
		var title = Strings.NormalizeNull(sn.WebsiteTitle);

		var sb = new StringBuilder();

		if (site is { }) {
			sb.Append(site);
		}

		if (title is { }) {
			sb.Append($" [{title}]");
		}

		site = sb.ToString().Trim(' ');

		/*var urls = sn.Urls.OrderByDescending(s =>
		{
			Url u = s;
			return u.Host == "gelbooru" || u.Host == "danbooru";
		}).ToArray();*/
		
		var urls = sn.Urls;

		var imageResult = new SearchResultItem(r)
		{
			Url         = urls[0],
			Similarity  = Math.Round(sn.Similarity, 2),
			Description = Strings.NormalizeNull(sn.Index.ToString()),
			Artist      = Strings.NormalizeNull(sn.Creator),
			Source      = Strings.NormalizeNull(sn.Material),
			Character   = Strings.NormalizeNull(sn.Character),
			Site        = site,
			Title       = Strings.NormalizeNull(sn.Title),
			Metadata    = urls[1..]
		};

		return imageResult;

	}

	/// <summary>
	/// Origin result
	/// </summary>
	private sealed class SauceNaoDataResult
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
		public double Similarity { get; internal set; }

		public string WebsiteTitle { get; internal set; }

		public string Title { get; internal set; }

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