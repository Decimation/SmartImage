using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Json;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;
using Kantan.Net.Utilities;
using Kantan.Text;

namespace SmartImage.Lib.Engines.Search.Other;

public sealed class BingEngine : BaseSearchEngine
{
	public BingEngine() : base("https://www.bing.com/images/searchbyimage?cbir=sbi&imgurl=") { }

	public override SearchEngineOptions EngineOption => SearchEngineOptions.Bing;

	public override void Dispose() { }

	// Parsing does not seem feasible ATM

	#region Overrides of BaseSearchEngine

	public async Task<SearchResult> SearchAltQueryAsync(string query)
	{
		var sr = new SearchResult(this)
		{
			RawUrl = GetAltQueryUrl(query),
		};

		var req = await sr.RawUrl.WithHeaders(new
		{
			User_Agent = HttpUtilities.UserAgent
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
			var j    = JsonValue.Parse(attr);

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

	#endregion
}