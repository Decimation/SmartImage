// Author: Deci | Project: SmartImage.Lib | Name: GoogleLens.cs
// Date: 2025/04/15 @ 11:04:23

using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Flurl.Http;
using Kantan.Net.Web;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Cookies;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Images.Uni;

// ReSharper disable UnusedMember.Local
#pragma warning disable IDE0051
namespace SmartImage.Lib.Engines.Search;

public class GoogleLensEngine : WebSearchEngine<GoogleLensItem, IList<INode>>, ICookiesReceiver
{

	// TODO: WIP

	public const string URL_BASE  = "https://lens.google.com/";
	public const string URL_BASE2 = "https://www.google.com/";

	public override string Name => "Google Lens";

	public override SearchEngineOptions Option => SearchEngineOptions.GoogleLens;


	public override Url Url => URL_BASE;

	public Url Endpoint => URL_BASE;

	public GoogleLensEngine() : base(URL_BASE)
	{
		Jar = new CookieJar();
	}

	public static readonly string[] SearchTypes = ["all", "products", "visual_matches", "exact_matches"];

	public string HlParam { get; set; } = "en-US";

	public string SearchType { get; set; } = SearchTypes[0];

	public object Headers = new
	{
		User_Agent      = R1.UserAgent1,
		Connection      = "keep-alive",
		Accept_Encoding = "gzip, deflate, br",
		Accept          = "*/*"
	};

	// public FlurlCookie Nid { get; set; }


	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken ct = default)
	{
		var br = await base.GetResultAsync(query, ct);

		return br;
	}

	protected override ValueTask<IList<INode>> ParseIntermediateAsync(IDocument src)
	{
		var nodes = src.QuerySelectorAll(".LBcIee").OfType<INode>().ToList();
		return ValueTask.FromResult<IList<INode>>(nodes);
	}

	protected override ValueTask<IEnumerable<GoogleLensItem>> ParseItemsAsync(IList<INode> source, SearchResult r)
	{
		var buf = new List<GoogleLensItem>(source.Count);

		foreach (INode node in source) {
			buf.Add(GoogleLensItem.ParseSource(node, r));
		}

		return ValueTask.FromResult<IEnumerable<GoogleLensItem>>(buf);
	}

	private Task<IFlurlResponse> SearchFileAsync(SearchQuery query, CancellationToken token)
	{
		string               endpoint;
		UniImageFile         uif      = query.Source as UniImageFile;
		string               filename = uif.LocalFileInfo.Name;
		Task<IFlurlResponse> req;
		endpoint = "v3/upload";

		// filename = "image.jpg";
		// filename = (query.Source is UniImageFile uif) ? uif.FileInfo.Name : "image.jpg";
		filename = uif.LocalFileInfo.Name;

		req = Client.Request(Endpoint, endpoint)
			.SetQueryParam("hl", HlParam)
			.WithTimeout(Timeout)
			.WithCookies(Jar)

			// .WithCookie(Nid.Name, Nid.Value)
			.WithHeaders(Headers)
			.PostMultipartAsync(bc =>
			{
				//
				bc.AddFile("encoded_image", uif.LocalFilePath, contentType: "image/jpeg", fileName: filename);
			}, cancellationToken: token);

		return req;
	}

	protected override async Task<IDocument> GetSourceAsync(SearchResult sr, SearchQuery query, CancellationToken token = default)
	{
		Task<IFlurlResponse> req = null;
		IFlurlResponse       res = null;

		//todo

		if (query.Source.IsUri) {
			req = SearchUrlAsync(query, token);
			res = await req.ConfigureAwait(false);
		}
		else if (query.Source.IsFile) {
			req = SearchFileAsync(query, token);
			res = await req.ConfigureAwait(false);
		}
		else {
			return null;
		}

		var requestMessage = res.ResponseMessage.RequestMessage;
		Logger.LogTrace("{Uri} {Code}", requestMessage?.RequestUri, res.StatusCode);

		// var stream = await res.GetStringAsync();
		/*var url = res.ResponseMessage.RequestMessage.RequestUri;

		using var res2 = await Client.Request(url)
			                 .WithTimeout(Timeout)
			                 .WithCookie(Nid.Name, Nid.Value)

			                 // .WithHeaders(Headers)
			                 .GetAsync(cancellationToken: token);

		var resData = await res2.GetStreamAsync();*/
		// var str = await res.GetStringAsync().ConfigureAwait(false);

		var resData = await res.GetStreamAsync().ConfigureAwait(false);

		var parser = new HtmlParser(new HtmlParserOptions()
		{
			IsScripting                         = true,
			IsStrictMode                        = false,
			IsAcceptingCustomElementsEverywhere = true,
			IsEmbedded                          = true
		});

		var doc = await parser.ParseDocumentAsync(resData).ConfigureAwait(false);

		// BrowsingContext.New(Configuration.Default.WithCookies().WithCss());

		return doc;
	}

	private Task<IFlurlResponse> SearchUrlAsync(SearchQuery query, CancellationToken token)
	{
		return SearchUrlAsync(query.Upload.Url, token);
	}

	private Task<IFlurlResponse> SearchUrlAsync(Url url, CancellationToken token)
	{
		string               endpoint;
		Task<IFlurlResponse> req;
		endpoint = "uploadbyurl";

		var req1 = Client.Request(Endpoint, endpoint)
			.SetQueryParam("hl", HlParam)
			.SetQueryParam("url", url)
			.WithCookies(Jar)

			// .WithCookie(Nid.Name, Nid.Value)
			.WithHeaders(Headers)
			.WithTimeout(Timeout);

		req = req1.GetAsync(cancellationToken: token);

		// var sz = await (await req).GetStringAsync();

		return req;
	}

	protected override Url GetRawUrl(SearchQuery query)
	{
		return base.GetRawUrl(query);
	}

	public override void Dispose() { }

	public CookieJar Jar { get; private set; }

	public async ValueTask<bool> ApplyCookiesAsync(ICookiesSource source, CancellationToken token = default)
	{
		if (source == null) {
			return false;
		}

		var ck   = await source.GetOrLoadCookiesAsync(token).ConfigureAwait(false);
		var nids = ck.OfType<FirefoxCookie>().Where(static x => x.Name == "NID" && x.Host.Contains("google.com"));
		var nid  = nids.FirstOrDefault();

		if (nid == null) {
			return false;
		}

		var nidFc = nid.AsFlurlCookie(URL_BASE);

		// Nid ??= nidFc;
		Jar.AddOrReplace(nidFc);


		return true;
	}

}

public class GoogleLensItem : SearchResultItem, IParseableSource<INode, GoogleLensItem>
{

	// public string SiteName { get; private set; }

	// public Url Link { get; private set; }

	public string Ping { get; private set; }


	private GoogleLensItem(SearchResult r) : base(r) { }

	public static GoogleLensItem ParseSource(INode n, SearchResult r)
	{
		var gli = new GoogleLensItem(r);

		if (n is IHtmlElement e) {
			var attrHref = e.Attributes["href"];
			var attrPing = e.Attributes["ping"];
			var title    = e.QuerySelector(".Yt787")?.TextContent;

			//e.QuerySelector("//*[class*='gdOPf q07dbf uhHOwf ez24Df']");
			// var siteName = e.SelectNodes("//*[contains(@class,'gdOPf')]");
			//R8BTeb q8U8x LJEGod du278d i0Rdmd
			var siteName = e.QuerySelector(".R8BTeb");
			gli.Url   = attrHref?.Value;
			gli.Title = title;
			gli.Ping  = attrPing?.Value;
			gli.Site  = siteName.TextContent;
		}

		return gli;
	}

}