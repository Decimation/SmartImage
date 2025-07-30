// Author: Deci | Project: SmartImage.Lib | Name: GoogleLens.cs
// Date: 2025/04/15 @ 11:04:23

using AngleSharp;
using AngleSharp.Css.Values;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Flurl.Http;
using Flurl.Http.Content;
using Kantan.Net.Utilities;
using Kantan.Net.Web;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Cookies;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Engines.Results.Model;
using SmartImage.Lib.Images.Uni;

// ReSharper disable UnusedMember.Local
#pragma warning disable IDE0051
namespace SmartImage.Lib.Engines.Search;

public record GoogleLensItem : SearchResultItem, ISourceItemParseable<INode, GoogleLensItem>
{

	public string SiteName { get; private set; }

	public Url Link { get; private set; }

	public string Ping { get; private set; }


	private GoogleLensItem(SearchResult r) : base(r) { }

	public static GoogleLensItem ParseResultItem(INode n, SearchResult r)
	{
		var gli = new GoogleLensItem(r);

		if (n is IHtmlElement e)
		{
			var attrHref = e.Attributes["href"];
			var attrPing = e.Attributes["ping"];
			var title    = e.QuerySelector(".Yt787")?.TextContent;

			//e.QuerySelector("//*[class*='gdOPf q07dbf uhHOwf ez24Df']");
			// var siteName = e.SelectNodes("//*[contains(@class,'gdOPf')]");
			//R8BTeb q8U8x LJEGod du278d i0Rdmd
			var siteName = e.QuerySelector(".R8BTeb");
			gli.Link     = attrHref?.Value;
			gli.Title    = title;
			gli.Ping     = attrPing?.Value;
			gli.SiteName = siteName.TextContent;
		}

		return gli;
	}

}

public class GoogleLensEngine : WebSearchEngine<GoogleLensItem, IList<INode>>, IEndpointUrl, ICookiesReceiver
{

	// TODO: WIP

	public const string URL_BASE  = "https://lens.google.com/";
	public const string URL_BASE2 = "https://www.google.com/";

	public override string Name => "Google Lens";

	public override SearchEngineOptions EngineOption => SearchEngineOptions.GoogleLens;


	public override Url BaseUrl => URL_BASE;

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
		User_Agent      = HttpUtilities.UserAgent,
		Connection      = "keep-alive",
		Accept_Encoding = "gzip, deflate, br",
		Accept          = "*/*"
	};

	// public FlurlCookie Nid { get; set; }


	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken token = default)
	{
		var br = await base.GetResultAsync(query, token);

		return br;
	}

	protected override ValueTask<IList<INode>> GetSource(IDocument d)
	{
		var nodes = d.QuerySelectorAll(".LBcIee").OfType<INode>().ToList();
		return ValueTask.FromResult<IList<INode>>(nodes);
	}

	protected override ValueTask<IEnumerable<GoogleLensItem>> GetItems(IList<INode> source, SearchResult r)
	{
		var buf = new List<GoogleLensItem>(source.Count);

		foreach (INode node in source)
		{
			buf.Add(GoogleLensItem.ParseResultItem(node, r));
		}

		return ValueTask.FromResult<IEnumerable<GoogleLensItem>>(buf);
	}

	private Task<IFlurlResponse> SearchFileAsync(SearchQuery query, CancellationToken token)
	{
		string               endpoint;
		UniImageFile         uif      = query.Source as UniImageFile;
		string               filename = uif.FileInfo.Name;
		Task<IFlurlResponse> req;
		endpoint = "v3/upload";

		// filename = "image.jpg";
		// filename = (query.Source is UniImageFile uif) ? uif.FileInfo.Name : "image.jpg";
		filename = uif.FileInfo.Name;

		req = Client.Request(Endpoint, endpoint)
			.SetQueryParam("hl", HlParam)
			.WithTimeout(Timeout)
			.WithCookies(Jar)

			// .WithCookie(Nid.Name, Nid.Value)
			.WithHeaders(Headers)
			.PostMultipartAsync(bc =>
			{
				//
				bc.AddFile("encoded_image", uif.FilePath, contentType: "image/jpeg", fileName: filename);
			}, cancellationToken: token);

		return req;
	}

	protected override async Task<IDocument> GetDocumentAsync(SearchResult sr, SearchQuery query, CancellationToken token = default)
	{
		string               endpoint, filename;
		Task<IFlurlResponse> req = null;

		IFlurlResponse res = null;

		//todo


		if (query.Source.IsUri)
		{

			req = SearchUrlAsync(query, token);
			res = await req.ConfigureAwait(false);
		}
		else if (query.Source.IsFile)
		{
			req = SearchFileAsync(query, token);
			res = await req.ConfigureAwait(false);
		}
		else
		{
			return null;
		}

		Logger.LogTrace("{Uri} {Code}", res.ResponseMessage.RequestMessage.RequestUri, res.StatusCode);

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
		return SearchUrlAsync(query.Upload, token);
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
		if (source == null)
		{
			return false;
		}

		var ck   = await source.GetOrLoadCookiesAsync(token).ConfigureAwait(false);
		var nids = ck.OfType<FirefoxCookie>().Where(x => x.Name == "NID" && x.Host.Contains("google.com"));
		var nid  = nids.FirstOrDefault();

		if (nid == null)
		{
			return false;
		}
		var nidFc = nid.AsFlurlCookie(URL_BASE);

		// Nid ??= nidFc;
		Jar.AddOrReplace(nidFc);


		return true;
	}

#region Implementation of ISearchConfigReceiver

	public override ValueTask<bool> ApplyConfigAsync(SearchConfig cfg, CancellationToken ct = default)
	{
		return ValueTask.FromResult(true);
	}

#endregion

}