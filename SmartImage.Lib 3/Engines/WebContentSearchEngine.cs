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

		var readStr = await origin.AllowAnyHttpStatus()
		                          .WithHeaders(new { User_Agent = HttpUtilities.UserAgent })
		                          /*.WithAutoRedirect(true)*/
		                          .GetStringAsync();

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
			r.Results.Add(sri);
		}

		Debug.WriteLine($"{r.RawUrl} {d.TextContent?.Length} {n.Count}", Name);
		return r;
	}

	#endregion

	protected abstract Task<IList<INode>> GetNodesAsync(IDocument doc);

	protected abstract Task<SearchResultItem> ParseResultItemAsync(INode n, SearchResult r);
}