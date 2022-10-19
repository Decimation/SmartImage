using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Flurl.Http;
using Kantan.Net.Utilities;

namespace SmartImage.Lib.Engines;

public abstract class WebContentSearchEngine : BaseSearchEngine
{
	protected WebContentSearchEngine(string baseUrl) : base(baseUrl) { }

	protected abstract string NodesSelector { get; }

	#region Overrides of BaseSearchEngine

	[ICBN]
	protected virtual async Task<IDocument> ParseDocumentAsync(Url origin)
	{
		var parser = new HtmlParser();

		try {
			var res = await origin.AllowAnyHttpStatus()
			                      .WithCookies(out var cj)
			                      .WithTimeout(Timeout)
			                      .WithHeaders(new
			                      {
				                      User_Agent = HttpUtilities.UserAgent
			                      })
			                      /*.WithAutoRedirect(true)*/
			                      /*.OnError(s =>
			                      {
				                      s.ExceptionHandled = true;
			                      })*/
			                      .GetAsync();

			var str = await res.GetStringAsync();

			var document = await parser.ParseDocumentAsync(str);

			return document;
		}
		catch (FlurlHttpException e) {
			// return await Task.FromException<IDocument>(e);
			Debug.WriteLine($"{e.Message}", Name);

			return null;
		}
	}

	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken? token = null)
	{
		token ??= CancellationToken.None;

		var result = await base.GetResultAsync(query, token);
		var doc    = await ParseDocumentAsync(result.RawUrl);

		if (doc is not { }) {
			result.Status = SearchResultStatus.Failure;
			return result;
		}

		var nodes = await GetNodesAsync(doc);

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

		FinalizeResult(result);

		return result;
	}

	#endregion
	
	protected virtual Task<List<INode>> GetNodesAsync(IDocument doc)
	{
		return Task.FromResult(doc.Body.SelectNodes(NodesSelector));
	}

	protected abstract Task<SearchResultItem> ParseResultItemAsync(INode n, SearchResult r);
}