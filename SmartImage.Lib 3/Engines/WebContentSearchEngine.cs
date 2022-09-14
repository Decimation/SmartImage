using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Flurl.Http;
using Kantan.Net.Utilities;

namespace SmartImage.Lib.Engines;

public abstract class WebContentSearchEngine : BaseSearchEngine
{
	protected WebContentSearchEngine(string baseUrl) : base(baseUrl) { }

	#region Overrides of BaseSearchEngine

	protected virtual async Task<IDocument> ParseDocumentAsync(Url origin)
	{
		var parser = new HtmlParser();

		var res = await origin.AllowAnyHttpStatus()
		                      .WithCookies(out var cj)
		                      .WithHeaders(new { User_Agent = HttpUtilities.UserAgent })
		                      /*.WithAutoRedirect(true)*/
		                      .GetAsync();

		var readStr = await res.GetStringAsync();

		var document = await parser.ParseDocumentAsync(readStr);

		return document;
	}

	public override async Task<SearchResult> GetResultAsync(SearchQuery query)
	{
		var r = await base.GetResultAsync(query);
		var d = await ParseDocumentAsync(r.RawUrl);
		var n = await GetNodesAsync(d);

		foreach (INode node in n) {
			var sri = await ParseResultItemAsync(node, r);
			if (SearchResultItem.Validate(sri)) {
				r.Results.Add(sri);
			}
			else {
				Debug.WriteLine($"{sri} failed validation", Name);
			}
		}

		Debug.WriteLine($"{r.RawUrl} {d.TextContent?.Length} {n.Count}", Name);
		return r;
	}

	#endregion

	protected abstract Task<IList<INode>> GetNodesAsync(IDocument doc);

	protected abstract Task<SearchResultItem> ParseResultItemAsync(INode n, SearchResult r);
}