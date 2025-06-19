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
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Results;
using SmartImage.Lib.Results.Data;

// ReSharper disable UnusedMember.Local
#pragma warning disable IDE0051
namespace SmartImage.Lib.Engines.Impl.Search;

public class GoogleLensItem : ISearchResultItemConverter<GoogleLensItem>
{

	public string Title { get; private set; }

	public string SiteName { get; private set; }

	public Url Link { get; private set; }

	public string Ping { get; private set; }

	public ValueTask<SearchResultItem> ToItem(SearchResult sr)
	{
		var sri = new SearchResultItem(sr)
		{
			Title = Title,
			Site  = SiteName,
			Url   = Link
		};


		return ValueTask.FromResult(sri);
	}

	public static GoogleLensItem Parse(INode n)
	{
		var gli = new GoogleLensItem();

		if (n is IHtmlElement e) {
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

public class GoogleLensEngine : WebSearchEngine, IEndpointEngine, ICookiesReceiver, ISearchConfigReceiver
{

	// TODO: WIP

	public const string URL_BASE  = "https://lens.google.com/";
	public const string URL_BASE2 = "https://www.google.com/";

	public override string Name => "Google Lens";

	public override SearchEngineOptions EngineOption => SearchEngineOptions.GoogleLens;

	protected override string NodesSelector => null;

	public override Url BaseUrl => URL_BASE;

	public Url EndpointUrl => URL_BASE;

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

	protected override ValueTask<SearchResultItem> ParseResultItem(INode n, SearchResult r)
	{
		var gli = GoogleLensItem.Parse(n);
		var sri = gli.ToItem(r);
		return sri;
	}

	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken token = default)
	{
		var br = await base.GetResultAsync(query, token);

		return br;
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

		req = Client.Request(EndpointUrl, endpoint)
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


		if (query.Source.IsUri) {

			req = SearchUrlAsync(query, token);
			res = await req;
		}
		else if (query.Source.IsFile) {
			req = SearchFileAsync(query, token);
			res = await req;
		}
		else {
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
		var str = await res.GetStringAsync();

		var resData = await res.GetStreamAsync();

		var parser = new HtmlParser(new HtmlParserOptions()
		{
			IsScripting                         = true,
			IsStrictMode                        = false,
			IsAcceptingCustomElementsEverywhere = true,
			IsEmbedded                          = true
		});

		var doc = await parser.ParseDocumentAsync(resData);

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

		var req1 = Client.Request(EndpointUrl, endpoint)
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

	protected override ValueTask<IEnumerable<INode>> GetNodes(IDocument d)
	{
		var all = d.QuerySelectorAll(".LBcIee");
		return ValueTask.FromResult(all.OfType<INode>());
	}

	protected override Url GetRawUrl(SearchQuery query)
	{
		return base.GetRawUrl(query);
	}

	public override void Dispose() { }

	public CookieJar Jar { get; private set; }

	public async ValueTask<bool> ApplyCookiesAsync(ICookiesProvider provider, CancellationToken token = default)
	{
		if (provider == null) {
			return false;
		}

		var ck   = await provider.GetOrLoadCookiesAsync(token);
		var nids = ck.OfType<FirefoxCookie>().Where(x => x.Name == "NID" && x.Host.Contains("google.com"));
		var nid  = nids.First();

		var nidFc = nid.AsFlurlCookie(URL_BASE);
		// Nid ??= nidFc;
		Jar.AddOrReplace(nidFc);


		return true;
	}

#region Implementation of ISearchConfigReceiver

	public ValueTask<bool> ApplyConfigAsync(SearchConfig cfg, CancellationToken ct = default)
	{
		return ValueTask.FromResult(true);
	}

#endregion

}