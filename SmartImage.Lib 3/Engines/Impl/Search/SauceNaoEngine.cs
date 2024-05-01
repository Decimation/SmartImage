// ReSharper disable UnusedMember.Global

using System.Diagnostics;
using System.Json;
using System.Net;
using System.Text;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Flurl.Http;
using Kantan.Net.Utilities;
using Kantan.Text;
using SmartImage.Lib.Model;
using SmartImage.Lib.Results;
using static Kantan.Diagnostics.LogCategories;
using static SmartImage.Lib.Engines.Impl.Search.SauceNaoEngine.SauceNaoConstants;
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

public sealed class SauceNaoEngine : BaseSearchEngine, IConfig, IDisposable
{

	private const string URL_BASE = "https://saucenao.com/";

	private const string URL_API = URL_BASE + "search.php";

	private const string URL_QUERY = $"{URL_API}?url=";

	/*
	 * Excerpts adapted from https://github.com/Lazrius/SharpNao/blob/master/SharpNao.cs#L53
	 * https://github.com/luk1337/SauceNAO/blob/master/app/src/main/java/com/luk/saucenao/MainActivity.java
	 */

	public SauceNaoEngine(string authentication) : base(URL_QUERY, URL_API)
	{
		Authentication = authentication;

	}

	public SauceNaoEngine() : this(null) { }

	public string Authentication { get; set; }

	public bool UsingAPI => !string.IsNullOrWhiteSpace(Authentication);

	public override SearchEngineOptions EngineOption => SearchEngineOptions.SauceNao;

	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken token = default)
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
			.SelectMany(x =>
			{
				var i = x.Convert(result, out var rg);

				Array.Resize(ref rg, rg.Length + 1);
				rg[^1] = i;

				return rg;
			})
			.Where(o => o != null)

			// .OrderByDescending(e => e.Similarity)
			.ToList();

		if (imageResults.Count == 0) {
			// No good results
			//return sresult;
			result.Status = SearchResultStatus.NoResults;

			goto ret;

		}

		result.Status = SearchResultStatus.Success;

		// TODO: HACK

		/*var allSisters = imageResults
				.SelectMany(ir => ir.Children)
				.DistinctBy(s => s.Url)
				.ToList(); // note: need ToList()

			for (int i = 0; i < imageResults.Count; i++) {
				var ir = imageResults[i];
				ir.Children.Clear();
				ir.Children.AddRange(allSisters.Where(irs => irs.Parent == ir));
			}*/

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

		response = await EndpointUrl.AllowHttpStatus()
			           .OnError(x =>
			           {

				           x.ExceptionHandled = true;

				           /*if (x.Exception is FlurlHttpException ex) {
						           if (ex.StatusCode == (int)HttpStatusCode.TooManyRequests) { }
					           }*/

				           // html = await ((FlurlHttpException) x.Exception).GetResponseStringAsync();
			           })
			           .WithTimeout(Timeout)
			           .PostMultipartAsync(m =>
			           {
				           m.AddString("url", query.IsUri ? query.Value.ToString() : string.Empty);

				           if (query.IsUri) { }
				           else if (query.IsFile) {
					           m.AddFile("file", query.Value.ToString(), fileName: "image.png");
				           }

			           });

		html = await response.GetStringAsync();

		/*
		 * Daily Search Limit Exceeded.
		 * <IP>, your IP has exceeded the unregistered user's daily limit of 100 searches.
		 */

		if (response.StatusCode == (int) HttpStatusCode.TooManyRequests) {
			Trace.WriteLine("On cooldown!", Name);
			return await Task.FromResult(Enumerable.Empty<SauceNaoDataResult>());
		}

		// html = await e.GetResponseStringAsync();

		/*try { }
		catch (FlurlHttpException e) { }*/

		/*var raw=await GetRawUrlAsync(query);
		var html2=await raw.GetStringAsync();*/

		var doc = await docp.ParseDocumentAsync(html);

		var results = doc.Body.SelectNodes("//div[@class='result']");

		return results.Select(Parse).ToList();
	}

	private static SauceNaoDataResult Parse(INode result)
	{
		// TODO: OPTIMIZE

		if (result == null) {
			return null;
		}

		const string HIDDEN_ID_VAL = "result-hidden-notification";

		if (result.TryGetAttribute(Serialization.Atr_id) == HIDDEN_ID_VAL) {
			return null;
		}

		var resultElem = result as IHtmlElement;
		var ri         = resultElem.QuerySelector("img");

		// var resultImg      = resultElem.QuerySelector(".resultimage");
		// var resultImg2     = resultImg.FirstChild.FirstChild;
		// var thumbnail      = resultImg2.TryGetAttribute("src");
		// var thumbnailTitle = resultImg2.TryGetAttribute("title");

		var thumbnail      = ri.GetAttribute("src");
		var thumbnailTitle = ri.GetAttribute("title");
		var pixelated      = ri.GetAttribute("class");
		var isPixelated    = pixelated == "pixelated";

		var ds = ri.Attributes.Where(x => x.Name.Contains("data-src")).ToArray();

		thumbnail = isPixelated ? ds.LastOrDefault()?.Value : thumbnail;

		var resulttablecontent = result.FirstChild
			.FirstChild
			.FirstChild
			.ChildNodes[1];

		var resultmatchinfo      = resulttablecontent.FirstChild;
		var resultsimilarityinfo = resultmatchinfo.FirstChild;

		// Contains links
		var resultmiscinfo = resultmatchinfo.ChildNodes[1];

		// var resultcontent  = resulttablecontent.ChildNodes[1];
		// var resultcontentcolumn = resultcontent.ChildNodes[1];
		var resultcontent = ((IElement) result).GetElementsByClassName("resultcontent")[0];

		IHtmlCollection<IElement> resultcontentcolumn_rg = null;

		if (result is IElement { } elem) {
			resultcontentcolumn_rg = elem.QuerySelectorAll(Serialization.S_SauceNao_ResultContentColumn);

		}

		// var resulttitle = resultcontent.ChildNodes[0];

		var links = new List<string>();

		if (resulttablecontent is IElement { } e) {
			var links1 = e.QuerySelectorAll(Serialization.Tag_a)
				.Select(x => x.GetAttribute(Serialization.Atr_href));
			links.AddRange(links1);
		}

		var element = resultcontentcolumn_rg.Select(c => c.ChildNodes)
			.SelectMany(c => c.GetElementsByTagName(Serialization.Tag_a)
				            .Select(x => x.GetAttribute(Serialization.Atr_href)))
			.Where(ec => ec != null);

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

		// string material1 = rcci.SubstringAfter(material);
		string material1 = rcci.SubstringAfter(Material);

		// string creator1 = rcci;
		string creator1 = rcci;

		// string characters1  = null;
		bool rtiHasArtist = false;

		foreach (var s in Syn_Artists) {
			if (rti.StartsWith(s)) {
				rti          = rti.SubstringAfter(s).Trim(' ');
				rtiHasArtist = true;
			}
		}

		var nodes = resultcontentcolumn_rg.SelectMany(e => e.ChildNodes)
			.Where(c => c is not (IElement { TagName: "BR" }
				            or IElement { NodeName: "SPAN" }))
			.ToArray();

		float similarity = float.Parse(resultsimilarityinfo.TextContent.Replace("%", string.Empty));

		var sndr = new SauceNaoDataResult
		{
			Urls           = links.Distinct().ToArray(),
			Similarity     = similarity,
			Material       = material1,
			Thumbnail      = thumbnail,
			ThumbnailTitle = thumbnailTitle

		};

		if (rtiHasArtist) {
			sndr.Creator = rti;
		}

		for (int i = 0; i < nodes.Length; i++) {
			var node = nodes[i];
			var s    = node.TextContent;

			if (s.StartsWith(Source)) {
				sndr.Source = nodes[++i].TextContent.Trim(' ');
				continue;
			}

			if (s.StartsWith(Material)) {
				sndr.Material = nodes[++i].TextContent.Trim(' ');
				continue;
			}

			if (Syn_Characters.Any(s.StartsWith)) {
				sndr.Character = nodes[++i].TextContent.Trim(' ');
				continue;
			}

			if (Syn_Artists.Any(s.StartsWith) || s.StartsWith(Twitter)) {
				sndr.Creator = nodes[++i].TextContent.Trim(' ');

				// var idx = Array.IndexOf(sndr.Urls, nodes[i].TryGetAttribute(Serialization.Atr_href));
			}

		}

		return sndr;
	}

	private async Task<IEnumerable<SauceNaoDataResult>> GetAPIResultsAsync(SearchQuery url)
	{
		Trace.WriteLine($"{Name} | API");

		// var client = new HttpClient();

		const string dbIndex = "999";

		// const string numRes  = "6";

		var values = new Dictionary<string, string>
		{
			{ "db", dbIndex },
			{ "output_type", "2" },
			{ "api_key", Authentication },
			{ "url", url.Upload },

			// { "numres", numRes }
		};

		var content = new FormUrlEncodedContent(values);

		var res = await URL_API.AllowAnyHttpStatus()
			          .WithTimeout(Timeout)
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

			return await Task.FromResult(buffer.ToArray()).ConfigureAwait(false);
		}

		return null;
	}

	internal static class SauceNaoConstants
	{

		public const string Twitter = "Twitter:";
		public const string TweetID = "Tweet ID:";

		public const string Material = "Material:";
		public const string Source   = "Source:";

		public static readonly string[] Syn_Artists = ["Creator(s):", "Creator:", "Member:", "Artist:", "Author:"];

		public static readonly string[] Syn_Characters = ["Characters:"];

	}

	public ValueTask ApplyAsync(SearchConfig cfg)
	{
		Authentication = cfg.SauceNaoKey;
		return ValueTask.CompletedTask;
	}

}

/// <summary>
/// Origin result
/// </summary>
public sealed class SauceNaoDataResult : IResultConvertable
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

	public string Source { get; internal set; }

	public Url Thumbnail { get; internal set; }

	public string ThumbnailTitle { get; internal set; }

	public SearchResultItem Convert(SearchResult r, out SearchResultItem[] children)
	{
		var    idxStr   = Index.ToString();
		string siteName = Index != 0 ? idxStr : null;

		var site  = Strings.NormalizeNull(siteName);
		var title = Strings.NormalizeNull(WebsiteTitle);

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

		string[] urls = (Urls != null)
			                ? Urls.Distinct().Where(s => !string.IsNullOrWhiteSpace(s)).ToArray()
			                : [];

		string[] meta = [];

		if ((urls.Length >= 2)) {
			meta = urls[1..].Where(u => !((Url) u).QueryParams.Contains("lookup_type")).ToArray();
		}

		var imageResult = new SearchResultItem(r)
		{
			Url        = urls.FirstOrDefault(),
			Similarity = Math.Round(Similarity, 2),

			// Similarity = Similarity,
			Description    = siteName,
			Artist         = Strings.NormalizeNull(Creator),
			Source         = Strings.NormalizeNull(Material),
			Character      = Strings.NormalizeNull(Character),
			Site           = site,
			Title          = Strings.NormalizeNull(Title),
			Metadata       = meta,
			Thumbnail      = Thumbnail,
			ThumbnailTitle = ThumbnailTitle

		};

		children = imageResult.AddChildren(meta);

		return imageResult;

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