using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Flurl.Http;
using JetBrains.Annotations;
using Kantan.Net.Utilities;

namespace SmartImage.Lib.Engines;

public abstract class WebSearchEngine : BaseSearchEngine
{
	protected WebSearchEngine([NotNull] string baseUrl) : base(baseUrl) { }

	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken? token = null)
	{
		token ??= CancellationToken.None;
		var res = await base.GetResultAsync(query, token);

		IDocument doc = await GetDocumentAsync(res.RawUrl, query: query, token: token.Value);

		if (doc is not { }) {
			res.Status = SearchResultStatus.Failure;
			goto ret;
		}

		var nodes = await GetNodes(doc);

		foreach (INode node in nodes) {
			if (token.Value.IsCancellationRequested) {
				break;
			}

			var sri = await ParseNodeToItem(node, res);

			if (SearchResultItem.IsValid(sri)) {
				res.Results.Add(sri);
			}
		}

		Debug.WriteLine($"{Name} :: {res.RawUrl} {doc.TextContent?.Length} {nodes.Length}",
		                nameof(GetResultAsync));

		ret:
		res.Update();
		return res;
	}

	protected virtual async Task<IDocument> GetDocumentAsync(object origin2, SearchQuery query,
	                                                         CancellationToken? token = null)
	{
		token ??= CancellationToken.None;

		var parser = new HtmlParser();

		try {
			if (origin2 is Url origin) {
				var res = await origin.WithClient(SearchClient.Client)
				                      .AllowAnyHttpStatus()
				                      .WithCookies(out var cj)
				                      .WithTimeout(Timeout)
				                      .WithHeaders(new
				                      {
					                      User_Agent = HttpUtilities.UserAgent
				                      })
				                      .WithAutoRedirect(true)
				                      /*.OnError(s =>
				                      {
					                      s.ExceptionHandled = true;
				                      })*/
				                      .GetAsync(cancellationToken: token.Value);

				var str = await res.GetStringAsync();

				var document = await parser.ParseDocumentAsync(str, token.Value);

				return document;

			}
			else {
				return null;
			}
		}
		catch (FlurlHttpException e) {
			// return await Task.FromException<IDocument>(e);
			Debug.WriteLine($"{this} :: {e.Message}", nameof(GetDocumentAsync));

			return null;
		}
	}

	protected abstract ValueTask<SearchResultItem> ParseNodeToItem(INode n, SearchResult r);

	protected virtual ValueTask<INode[]> GetNodes(IDocument d)
		=> ValueTask.FromResult(d.Body.SelectNodes(NodesSelector).ToArray());

	protected abstract string NodesSelector { get; }
}