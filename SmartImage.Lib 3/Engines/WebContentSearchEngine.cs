using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Flurl.Http;

namespace SmartImage.Lib.Engines;

public abstract class WebContentSearchEngine : BaseSearchEngine
{
	protected WebContentSearchEngine(string baseUrl) : base(baseUrl) { }

	#region Overrides of BaseSearchEngine

	protected virtual async Task<IDocument> ParseDocumentAsync(Url origin)
	{
		var parser  = new HtmlParser();
		var readStr = await origin.GetStringAsync();

		var document = await parser.ParseDocumentAsync(readStr);

		return document;
	}

	public override async Task<SearchResult> GetResultAsync(SearchQuery query)
	{
		var u = await GetRawUrlAsync(query);
		var d = await ParseDocumentAsync(u);
		var n = await GetNodesAsync(d);

		var r = new SearchResult();

		foreach (INode node in n) {
			var sri = await ParseResultItemAsync(node, r);
			r.Results.Add(sri);
		}

		return r;
	}

	#endregion

	protected abstract Task<IEnumerable<INode>> GetNodesAsync(IDocument doc);

	protected abstract Task<SearchResultItem> ParseResultItemAsync(INode n, SearchResult r);
}