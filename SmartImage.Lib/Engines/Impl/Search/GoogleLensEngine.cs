// Author: Deci | Project: SmartImage.Lib | Name: GoogleLens.cs
// Date: 2025/04/15 @ 11:04:23

using AngleSharp;
using AngleSharp.Css.Values;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Flurl.Http;
using Kantan.Net.Utilities;
using Kantan.Net.Web;
using SmartImage.Lib.Cookies;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Results;
using SmartImage.Lib.Results.Data;

namespace SmartImage.Lib.Engines.Impl.Search;

public class GoogleLensItem : IResultConverter2<GoogleLensItem>
{

	public string Title { get; private set; }

	public string SiteName { get; private set; }

#region Implementation of IResultConverter2<out GoogleLensItem>

	public IEnumerable<SearchResultItem> ToResultItem(SearchResult sr)
	{
		var sri = new SearchResultItem(sr)
		{

		};

		return [sri];
	}

	public static GoogleLensItem Parse(INode n)
	{
		var gli = new GoogleLensItem();

		if (n is IHtmlElement e) {
			var title = e.QuerySelector(".Yt787")?.TextContent;

			//e.QuerySelector("//*[class*='gdOPf q07dbf uhHOwf ez24Df']");
			var siteName = e.SelectNodes("//*[contains(@class,'gdOPf')]");
			gli.Title    = title;
			gli.SiteName = siteName[0].TextContent;
		}

		return gli;
	}

#endregion

}

public class GoogleLensEngine : WebSearchEngine, IEndpointEngine /*, ICookiesEngine*/
{

	// TODO: WIP

	public const string URL_BASE = "https://lens.google.com";

	public override string Name => "Google Lens";

	public override Url BaseUrl => URL_BASE;

	public override SearchEngineOptions EngineOption => SearchEngineOptions.GoogleLens;

	protected override string NodesSelector => throw new NotImplementedException();

	public Url EndpointUrl => URL_BASE;

	public GoogleLensEngine() : base(URL_BASE) { }

	public FlurlCookie Nid { get; set; }

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

	protected override async ValueTask<SearchResultItem> ParseResultItem(INode n, SearchResult r)
	{


		var sri = new SearchResultItem(r)
			{ };

		return sri;
	}

	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken token = default)
	{
		var br = await base.GetResultAsync(query, token);
		return br;
	}

	protected override async Task<IDocument> GetDocumentAsync(SearchResult sr, SearchQuery query, CancellationToken token = default)
	{
		string               endpoint, filename;
		Task<IFlurlResponse> req;
		IFlurlResponse       res;

		if (query.Source.IsFile) { }
		else if (query.Source.IsUri) { }
		else {
			return null;
		}

		req = SearchUrlAsync(query, token);

		res = await req;

		// var stream = await res.GetStringAsync();
		var url = res.ResponseMessage.RequestMessage.RequestUri;

		var res2 = await Client.Request(url)
			           .WithTimeout(Timeout)
			           .WithCookie(Nid.Name, Nid.Value)

			           // .WithHeaders(Headers)
			           .GetAsync(cancellationToken: token);

		var resData = await res2.GetStringAsync();

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

	private Task<IFlurlResponse> SearchFileAsync(SearchQuery query, CancellationToken token)
	{
		string               endpoint;
		string               filename;
		UniImageFile         uif = query.Source as UniImageFile;
		Task<IFlurlResponse> req;
		endpoint = "v3/upload";

		// filename = "image.jpg";
		// filename = (query.Source is UniImageFile uif) ? uif.FileInfo.Name : "image.jpg";
		filename = uif.FileInfo.Name;

		req = Client.Request(EndpointUrl, endpoint)
			.SetQueryParam("hl", HlParam)
			.WithTimeout(Timeout)
			.WithCookie(Nid.Name, Nid.Value)
			.WithHeaders(Headers)
			.PostMultipartAsync(bc =>
			{
				//
				bc.AddFile(filename, uif.FilePath);
			}, cancellationToken: token);

		return req;
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

		req = Client.Request(EndpointUrl, endpoint)
			.SetQueryParam("hl", HlParam)
			.SetQueryParam("url", url)
			.WithCookie(Nid.Name, Nid.Value)
			.WithHeaders(Headers)
			.WithTimeout(Timeout)
			.GetAsync(cancellationToken: token);

		// var sz = await (await req).GetStringAsync();

		return req;
	}

	protected override ValueTask<List<INode>> GetNodes(IDocument d)
	{
		var all = d.QuerySelectorAll(".LBcIee");
		return ValueTask.FromResult(all.OfType<INode>().ToList());
	}

	protected override Url GetRawUrl(SearchQuery query)
	{
		return base.GetRawUrl(query);
	}

	public override void Dispose() { }

}