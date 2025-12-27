using System.Text.Json;
using System.Text.Json.Nodes;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Flurl;
using Flurl.Http;
using Kantan.Net.Utilities;
using Kantan.Text;
using SmartImage.Lib.Engines.Results;

namespace SmartImage.Lib.Engines.Search.Other;

public sealed class BingEngine : BaseSearchEngine
{

	public BingEngine() : base("https://www.bing.com/images/searchbyimage?cbir=sbi&imgurl=") { }

	

	public override SearchEngineOptions EngineOption => SearchEngineOptions.Bing;

	
	public override void Dispose() { }

	// Parsing does not seem feasible ATM

	public async Task<SearchResult> SearchAltQueryAsync(string query)
	{
		var rawUrl = GetAltQueryUrl(query);

		var sr = new SearchResult(this, rawUrl)
			{ };

		var req = await sr.RawUrl.WithHeaders(new
		{
			User_Agent = R1.UserAgent1
		}).GetAsync();

		var parser = new HtmlParser();
		var s      = await req.GetStringAsync();
		var doc    = await parser.ParseDocumentAsync(s);

		var elem = doc.QuerySelectorAll(".iuscp");

		foreach (IElement e in elem) {
			var imgpt = e.FirstChild;

			if (imgpt is IElement { ClassName: "tit" }) {
				continue;
			}

			var iusc = imgpt.FirstChild;
			var attr = iusc.TryGetAttribute("m");
			var j    = JsonNode.Parse(attr);

			var infopt = e.ChildNodes[1];

			sr.Results.Add(new SearchResultItem(sr)
			{
				Url         = j["murl"].ToString().CleanString(),
				Description = infopt.TextContent
			});
		}

		return sr;
	}

	private const string ALT_QUERY_URL = "https://www.bing.com/images/async";

	private static Url GetAltQueryUrl(string query, int cnt = 35)
	{
		var url = ALT_QUERY_URL.SetQueryParams(new
		{
			q     = query,
			first = 0,
			count = cnt,

			// qft   = @""""
		});
		return url;
	}

}