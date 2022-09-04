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
		var r = await base.GetResultAsync(query);
		var d = await ParseDocumentAsync(r.RawUrl);
		var n = await GetNodesAsync(d);

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