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
		                      .WithTimeout(Timeout)
		                      .WithHeaders(new { User_Agent = HttpUtilities.UserAgent })
		                      /*.WithAutoRedirect(true)*/
		                      .GetAsync();

		var readStr = await res.GetStringAsync();

		var document = await parser.ParseDocumentAsync(readStr);

		return document;
	}

	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken? token = null)
	{
		token ??= CancellationToken.None;

		var result = await base.GetResultAsync(query, token);
		var doc    = await ParseDocumentAsync(result.RawUrl);
		var nodes  = await GetNodesAsync(doc);

		foreach (INode node in nodes) {
			if (token.Value.IsCancellationRequested) {
				break;
			}

			var sri = await ParseResultItemAsync(node, result);

			if (SearchResultItem.Validate(sri)) {
				result.Results.Add(sri);
			}
			else {
				Debug.WriteLine($"{sri} failed validation", Name);
			}
		}

		Debug.WriteLine($"{result.RawUrl} {doc.TextContent?.Length} {nodes.Count}", Name);

		return result;
	}

	#endregion

	protected abstract Task<IList<INode>> GetNodesAsync(IDocument doc);

	protected abstract Task<SearchResultItem> ParseResultItemAsync(INode n, SearchResult r);
}