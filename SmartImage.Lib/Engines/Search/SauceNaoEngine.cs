// ReSharper disable UnusedMember.Global

using System.Diagnostics;
using System.Net;
using System.Text.Json.Nodes;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Flurl.Http;
using Kantan.Net.Utilities;
using Kantan.Text;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Model;

// ReSharper disable PossibleNullReferenceException
// ReSharper disable PropertyCanBeMadeInitOnly.Local
// ReSharper disable StringLiteralTypo
// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable ParameterTypeCanBeEnumerable.Local

namespace SmartImage.Lib.Engines.Search;

public sealed class SauceNaoEngine : WebSearchEngine<SauceNaoResultItem, IList<INode>>, IDisposable, ISearchConfigReceiver
{

	private const string URL_BASE = "https://saucenao.com/";

	private const string URL_API = URL_BASE + "search.php";

	private const string URL_QUERY = $"{URL_API}?url=";

	/*
	 * Excerpts adapted from https://github.com/Lazrius/SharpNao/blob/master/SharpNao.cs#L53
	 * https://github.com/luk1337/SauceNAO/blob/master/app/src/main/java/com/luk/saucenao/MainActivity.java
	 */

	protected override string[] ErrorBodyMessages { get; } = [];

	public Url Endpoint => URL_API;

	public bool UsingAPI => !String.IsNullOrWhiteSpace(Authentication);

	public string Authentication { get; set; }

	public override SearchEngineOptions Option => SearchEngineOptions.SauceNao;

	public SauceNaoEngine(string authentication = null) : base(URL_QUERY)
	{
		Authentication = authentication;

	}

	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken ct = default)
	{
		// var result = await base.GetResultAsync(query, token);
		var b = VerifyQuery(query);

		var srs = b ? SearchResultStatus.None : SearchResultStatus.IllegalInput;

		var rawUrl = GetRawUrl(query);

		var result = new SearchResult(this, rawUrl)
		{
			Status = srs,
		};

		if (UsingAPI) {
			Logger.LogInformation("[{Name}] API key: {Auth}", Name, Authentication);

			await GetAPIResultsAsync(query, result).ConfigureAwait(false);
		}
		else {

			var src = await GetSourceAsync(result, query, ct).ConfigureAwait(false);

			if (src is null || result is { Status: SearchResultStatus.Cooldown }) {
				goto ret1;
			}

			var source = await ParseIntermediateAsync(src);
			var items  = await ParseItemsAsync(source, result);
			result.Results.AddRange(items);
		}


	ret1:

		if (!result.HasResults) {
			result.ErrorMessage = "Daily search limit (50) exceeded";
			result.Status       = SearchResultStatus.Cooldown;

			//return sresult;
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

	ret:

		result.Update();

		return result;
	}

	protected override async Task<IDocument> GetSourceAsync(SearchResult sr, SearchQuery query, CancellationToken token = default)
	{
		Logger.LogTrace("[{Name}] Parsing HTML", Name);

		var docp = new HtmlParser();

		string         html     = null;
		IFlurlResponse response = null;

		response = await Client.Request(Endpoint).WithTimeout(Timeout).PostMultipartAsync(m =>
		{
			m.AddString("url", query.Source.IsUri ? query.Source.Value : String.Empty);
			string s;

			if (query.Source.IsUri) { }
			else {
				if (query.Source.IsFile) {
					s = query.Source.Value;
				}
				else {
					s = query.Source.LocalFilePath;
				}

				m.AddFile("file", s, fileName: "image.png");
			}

		}, cancellationToken: token).ConfigureAwait(false);

		html = await response.GetStringAsync().ConfigureAwait(false);

		/*
		 * Daily Search Limit Exceeded.
		 * <IP>, your IP has exceeded the unregistered user's daily limit of 100 searches.
		 */

		IHtmlDocument doc = null;

		if (response.StatusCode == (int) HttpStatusCode.TooManyRequests) {
			Logger.LogWarning("[{Name}] Parsing HTML", Name);

			sr.Status       = SearchResultStatus.Cooldown;
			sr.ErrorMessage = "On cooldown!";
			sr.Flags        = SearchResultFlags.NoResults;
			goto ret;
		}

		doc = await docp.ParseDocumentAsync(html).ConfigureAwait(false);

	ret:
		response.Dispose();

		return doc;
	}

	protected override ValueTask<IList<INode>> ParseIntermediateAsync(IDocument src)
	{
		var results = src.Body.SelectNodes("//div[@class='result']");

		return ValueTask.FromResult<IList<INode>>(results);
	}

	protected override ValueTask<IEnumerable<SauceNaoResultItem>> ParseItemsAsync(IList<INode> source, SearchResult r)
	{
		var buf = new List<SauceNaoResultItem>(source.Count);

		foreach (INode node in source) {
			var sndr = SauceNaoResultItem.ParseSource(node, r);

			buf.AddRange(sndr);
		}

		Logger.LogDebug("Disposing {Name} doc", Name);
		return ValueTask.FromResult<IEnumerable<SauceNaoResultItem>>(buf);
	}


	private async ValueTask GetAPIResultsAsync(SearchQuery url, SearchResult sr)
	{
		Logger.LogTrace("[{Name}] Using API", Name);

		const string dbIndex = "999";

		// const string numRes  = "6";

		var values = new Dictionary<string, string>
		{
			{ "db", dbIndex },
			{ "output_type", "2" },
			{ "api_key", Authentication },
			{ "url", url.Upload.Url },

			// { "numres", numRes }
		};

		var content = new FormUrlEncodedContent(values);

		var res = await Client.Request(URL_API)
		                      .WithTimeout(Timeout)
		                      .PostAsync(content).ConfigureAwait(false);

		var c = await res.GetStringAsync().ConfigureAwait(false);

		if (res.ResponseMessage.StatusCode == HttpStatusCode.Forbidden) {
			// return;
			goto ret;
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

		var jsonString = JsonNode.Parse(c);

		if (jsonString is JsonObject jsonObject) {
			var jsonArray = jsonObject[KeyResults].AsArray();

			for (int i = 0; i < jsonArray.Count; i++) {
				var    header = jsonArray[i][KeyHeader];
				var    data   = jsonArray[i][KeyData];
				string obj    = header.ToString();
				obj          =  obj[..^1];
				obj          += data.ToString()[1..].Insert(0, ",");
				jsonArray[i] =  JsonNode.Parse(obj);
			}

			string json = jsonArray.ToString();

			// var buffer      = new List<SearchResultItem>();
			var resultArray = JsonNode.Parse(json).AsArray();

			foreach (JsonNode t in resultArray) {
				var   result     = t.AsObject();
				float similarity = Single.Parse(result[KeySimilarity].AsValue().ToString());

				string[] strings = result.ContainsKey(KeyUrls)
					                   ? [.. (result[KeyUrls] as JsonArray).Select(static j => j.ToString().CleanString())]
					                   : null;

				var index = (SauceNaoSiteIndex) Int32.Parse(result[KeyIndex].ToString());

				foreach (string t1 in strings) {
					var item = new SearchResultItem(sr)
					{
						Url        = t1,
						Similarity = similarity,
						Site       = index.ToString(),
						Artist     = result.TryGetKeyValue(KeyCreator)?.ToString().CleanString(),
						Character  = result.TryGetKeyValue(KeyCharacters)?.ToString().CleanString(),
						Source     = result.TryGetKeyValue(KeyMaterial)?.ToString().CleanString()
					};
					sr.Results.Add(item);
				}
			}

			goto ret;

			// return;
		}

	ret:
		res.Dispose();
		return;
	}

	public ValueTask<bool> ApplyConfigAsync(SearchConfig cfg, CancellationToken ct = default)
	{
		Authentication = cfg.SauceNaoKey;
		return ValueTask.FromResult(UsingAPI);
	}

	public override void Dispose() { }

	public static bool IsLookupUrl(Url url)
	{
		return url.QueryParams.Contains("lookup_type");
	}

}

/// <summary>
/// Origin result
/// </summary>
public sealed record SauceNaoResultItem : SearchResultItem
{

	private SauceNaoResultItem() : this(null, false) { }

	private SauceNaoResultItem(SearchResult r, bool isRaw = false) : base(r, isRaw) { }

	/// <summary>
	///     The url(s) where the source is from. Multiple will be returned if the exact same image is found in multiple places
	/// </summary>
	public string[] Urls { get; internal set; }

	/// <summary>
	///     The search index of the image
	/// </summary>
	public SauceNaoSiteIndex Index { get; internal set; }

	internal const string KEY_TWITTER = "Twitter:";

	internal const string KEY_TWEET_ID = "Tweet ID:";

	internal const string KEY_MATERIAL = "Material:";

	internal const string KEY_SOURCE = "Source:";

	internal static readonly string[] Keys_Artist = ["Creator(s):", "Creator:", "Member:", "Artist:", "Author:"];

	internal static readonly string[] Keys_Characters = ["Characters:"];


	/*public SearchResultItem Convert(SearchResult r)
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
			}).ToArray();#1#

		string[] urls = (Urls != null)
			                ? Urls.Distinct().Where(s => !string.IsNullOrWhiteSpace(s)).ToArray()
			                : [];

		string[] meta = [];

		if ((urls.Length >= 2)) {
			meta = urls[1..].Where(u => !SauceNaoEngine.IsLookupUrl((Url) u)).ToArray();
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

		var children = imageResult.CreateChildren(meta);

		r.Results.AddRange(children);

		return imageResult;

	}*/

	public static IEnumerable<SauceNaoResultItem> ParseSource(INode result, SearchResult r)
	{
		// TODO: OPTIMIZE
		const string HIDDEN_ID_VAL = "result-hidden-notification";

		if (result == null || result.TryGetAttribute(Serialization.Atr_id) == HIDDEN_ID_VAL) {
			return [];
		}

		var sndr    = new SauceNaoResultItem(r);
		var results = new List<SauceNaoResultItem>();

		var resultElem = result as IHtmlElement;
		var ri         = resultElem.QuerySelector("img");

		string thumbnail = null, thumbnailTitle = null;

		if (ri != null) {
			// var resultImg      = resultElem.QuerySelector(".resultimage");
			// var resultImg2     = resultImg.FirstChild.FirstChild;
			// var thumbnail      = resultImg2.TryGetAttribute("src");
			// var thumbnailTitle = resultImg2.TryGetAttribute("title");
			thumbnail      = ri.GetAttribute("src");
			thumbnailTitle = ri.GetAttribute("title");
			var pixelated   = ri.GetAttribute("class");
			var isPixelated = pixelated == "pixelated";
			var ds          = ri.Attributes.Where(static x => x.Name.Contains("data-src")).ToArray();
			thumbnail = isPixelated ? ds.LastOrDefault()?.Value : thumbnail;
		}


		var resulttablecontent   = result.FirstChild.FirstChild.FirstChild.ChildNodes[1];
		var resultmatchinfo      = resulttablecontent.FirstChild;
		var resultsimilarityinfo = resultmatchinfo.FirstChild;

		// Contains links
		var resultmiscinfo = resultmatchinfo.ChildNodes[1];

		// var resultcontent  = resulttablecontent.ChildNodes[1];
		// var resultcontentcolumn = resultcontent.ChildNodes[1];
		var                       resultcontent          = ((IElement) result).GetElementsByClassName("resultcontent")[0];
		IHtmlCollection<IElement> resultcontentcolumn_rg = null;

		if (result is IElement { } elem) {
			resultcontentcolumn_rg = elem.QuerySelectorAll(Serialization.S_SauceNao_ResultContentColumn);
		}

		// var resulttitle = resultcontent.ChildNodes[0];
		var links = new List<string>();

		if (resulttablecontent is IElement { } e) {
			var links1 = e.QuerySelectorAll(Serialization.Tag_a)
			              .Select(static x => x.GetAttribute(Serialization.Atr_href));
			links.AddRange(links1);
		}

		var element = resultcontentcolumn_rg.Select(static c => c.ChildNodes)
		                                    .SelectMany(static c => c.GetElementsByTagName(Serialization.Tag_a)
		                                                             .Select(static x => x.GetAttribute(Serialization.Atr_href)))
		                                    .Where(static ec => ec != null);

		if (element.Any()) {
			links.AddRange(element);
		}

		if (resultmiscinfo != null) {
			links.Add(resultmiscinfo.ChildNodes.GetElementsByTagName(Serialization.Tag_a)
			                        .FirstOrDefault(static x => x.GetAttribute(Serialization.Atr_href) != null)?
			                        .GetAttribute(Serialization.Atr_href));
		}

		//	//div[contains(@class, 'resulttitle')]
		//	//div/node()[self::strong]
		INode  resulttitle = resultcontent.ChildNodes[0];
		string rti         = resulttitle?.TextContent;

		// INode  resultcontentcolumn1 = resultcontent.ChildNodes[1];
		string rcci = resultcontentcolumn_rg.FuncJoin(static e => e.TextContent, ",");

		// string material1 = rcci.SubstringAfter(material);
		string material1 = rcci.SubstringAfter(KEY_MATERIAL);

		// string creator1 = rcci;
		string creator1 = rcci;

		// string characters1  = null;
		bool rtiHasArtist = false;

		foreach (var s in Keys_Artist) {
			if (rti.StartsWith(s)) {
				rti          = rti.SubstringAfter(s).Trim(' ');
				rtiHasArtist = true;
			}
		}

		if (rtiHasArtist && String.IsNullOrWhiteSpace(sndr.Artist)) {
			// sndr.Creator = rti;
			// Debugger.Break();
			sndr.Artist = rti;
		}


		var rccNodes = resultcontentcolumn_rg.SelectMany(static e => e.ChildNodes)
		                                     .Where(static c => c is not (IElement { TagName: "BR" } or IElement { NodeName: "SPAN" }))
		                                     .ToArray();

		for (int i = 0; i < rccNodes.Length; i++) {
			var node     = rccNodes[i];
			var nodeText = node.TextContent;

			if (nodeText.StartsWith(KEY_SOURCE) || nodeText.StartsWith(KEY_MATERIAL)) {
				sndr.Source = rccNodes[++i].TextContent.Trim(' ');
				continue;
			}

			if (Keys_Characters.Any(nodeText.StartsWith)) {
				sndr.Character = rccNodes[++i].TextContent.Trim(' ');
				continue;
			}

			if (Keys_Artist.Any(nodeText.StartsWith) || nodeText.StartsWith(KEY_TWITTER)) {
				sndr.Artist = rccNodes[++i].TextContent.Trim(' ');
			}
		}

		float similarity = Single.Parse(resultsimilarityinfo.TextContent.Replace("%", String.Empty));

		var urls = links.Where(static x =>
		{
			var b = !String.IsNullOrWhiteSpace(x);
			var c = true;

			if (b) {
				c = !SauceNaoEngine.IsLookupUrl(Url.Parse(x));
			}

			return b && c;
		}).Distinct().ToArray();


		sndr.Similarity     = Math.Round(similarity, 2);
		sndr.Source         = material1;
		sndr.Thumbnail      = thumbnail;
		sndr.ThumbnailTitle = thumbnailTitle;
		sndr.Title          = rti;

		for (int i = 0; i < urls.Length; i++) {
			Url    url  = urls[i];
			string site = null;

			if (Url.IsValid(url)) {
				site = url.Host.Replace("www", "");
				site = site.Split('.', StringSplitOptions.RemoveEmptyEntries)[0];

			}

			// var sndri = sndr.With(url);
			var sndri = sndr with { Url = url, Site = site};
			results.Add(sndri);
		}

		/*sr.Results.Add(sndr);

		if (urls.Length >= 1) {
			var children = sndr.CreateChildren(urls[1..]);
			sr.Results.AddRange(children);
		}*/

		return results;
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