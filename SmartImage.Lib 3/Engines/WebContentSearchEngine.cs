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
	protected virtual async Task<IDocument> ParseDocumentAsync(Url origin, CancellationToken token)
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
			                      .GetAsync(token);

			var str = await res.GetStringAsync();

			var document = await parser.ParseDocumentAsync(str, token);

			return document;
		}
		catch (FlurlHttpException e) {
			// return await Task.FromException<IDocument>(e);
			Debug.WriteLine($"{Name} :: {e.Message}", nameof(ParseDocumentAsync));

			return null;
		}
	}

	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken? token = null)
	{
		var sw = Stopwatch.StartNew();

		token ??= CancellationToken.None;

		var result = await base.GetResultAsync(query, token);
		var doc    = await ParseDocumentAsync(result.RawUrl, token.Value);

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
		}

		Debug.WriteLine($"{Name} :: {result.RawUrl} {doc.TextContent?.Length} {nodes.Count}", nameof(GetResultAsync));

		result.Update();
		sw.Stop();

		return result;
	}

	#endregion

	protected virtual Task<List<INode>> GetNodesAsync(IDocument doc)
	{
		return Task.FromResult(doc.Body.SelectNodes(NodesSelector));
	}

	protected abstract Task<SearchResultItem> ParseResultItemAsync(INode n, SearchResult r);
}