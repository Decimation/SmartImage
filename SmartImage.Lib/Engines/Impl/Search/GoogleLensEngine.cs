// Author: Deci | Project: SmartImage.Lib | Name: GoogleLens.cs
// Date: 2025/04/15 @ 11:04:23

using AngleSharp;
using AngleSharp.Css.Values;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Flurl.Http;
using Kantan.Net.Utilities;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Results;

namespace SmartImage.Lib.Engines.Impl.Search;

public class GoogleLensEngine : WebSearchEngine, IEndpointEngine
{

	// TODO: WIP

	public const string URL_BASE = "https://lens.google.com";

	public override string Name => "Google Lens";

	public override Url BaseUrl => URL_BASE;

	public override SearchEngineOptions EngineOption => SearchEngineOptions.GoogleLens;

	protected override string NodesSelector => throw new NotImplementedException();

	public Url EndpointUrl => URL_BASE;

	public GoogleLensEngine() : base(URL_BASE) { }

	public static readonly string[] SearchTypes = ["all", "products", "visual_matches", "exact_matches"];

	public FlurlCookie Nid { get; set; }

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
		throw new NotImplementedException();
	}

	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken token = default)
	{
		return await base.GetResultAsync(query, token);
	}

	protected override async Task<IDocument> GetDocumentAsync(SearchResult sr, SearchQuery query, CancellationToken token = default)
	{
		string               endpoint, filename;
		Task<IFlurlResponse> req;
		IFlurlResponse       res;

		/*if (query.Source.IsFile) {
			req = SearchFile(query, token);
		}
		else if (query.Source.IsUri) {
			req = SearchUrl(query, token);
		}
		else {
			return null;
		}*/

		req = SearchUrlAsync(query, token);

		res = await req;

		// var stream = await res.GetStringAsync();
		var url = res.ResponseMessage.RequestMessage.RequestUri;

		var res2 = await Client.Request(url)
			           .WithTimeout(Timeout)
			           // .WithHeaders(Headers)
			           .WithCookie(Nid.Name, Nid.Value)
			           .GetAsync(cancellationToken: token);

		var resData = await res2.GetStringAsync();

		var parser = new HtmlParser(new HtmlParserOptions()
		{
			IsScripting = true, 
			IsStrictMode = false, 
			IsAcceptingCustomElementsEverywhere = true, 
			IsEmbedded = true
		});

		var doc    = await parser.ParseDocumentAsync(resData);

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
		return req;
	}

	protected override async ValueTask<List<INode>> GetNodes(IDocument d)
	{
		return await base.GetNodes(d);
	}

	protected override Url GetRawUrl(SearchQuery query)
	{
		return base.GetRawUrl(query);
	}

	public override void Dispose() { }

}