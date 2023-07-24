using System.Diagnostics;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Flurl.Http;
using JetBrains.Annotations;
using Kantan.Net.Utilities;
using SmartImage.Lib.Model;
using SmartImage.Lib.Results;

namespace SmartImage.Lib.Engines;

public abstract class WebSearchEngine : BaseSearchEngine
{
	protected WebSearchEngine([NotNull] string baseUrl) : base(baseUrl) { }

	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken token = default)
	{
		var res = await base.GetResultAsync(query, token);

		IDocument doc;

		try {
			doc = await GetDocumentAsync(res.RawUrl, query: query, token: token);
		}
		catch (Exception e) {
			Debug.WriteLine($"{e.Message}", nameof(GetResultAsync));
			doc = null;

		}

		if (!Validate(doc, res)) {
			goto ret;
		}

		var nodes = await GetNodes(doc);

		foreach (INode node in nodes) {
			if (token.IsCancellationRequested) {
				break;
			}

			var sri = await ParseResultItem(node, res);

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

	[ItemCanBeNull]
	protected virtual async Task<IDocument> GetDocumentAsync(object sender, SearchQuery query,
	                                                         CancellationToken token = default)
	{

		var parser = new HtmlParser();

		try {
			if (sender is Url origin) {
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
					          .GetAsync(cancellationToken: token);

				var str = await res.GetStringAsync();

				var document = await parser.ParseDocumentAsync(str, token);

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

	protected abstract ValueTask<SearchResultItem> ParseResultItem(INode n, SearchResult r);

	protected virtual ValueTask<INode[]> GetNodes(IDocument d)
		=> ValueTask.FromResult(d.Body.SelectNodes(NodesSelector).ToArray());

	protected abstract string NodesSelector { get; }

	protected bool Validate([CBN] IDocument doc, SearchResult sr)
	{
		if (doc is null or { Body: null }) {
			sr.Status = SearchResultStatus.Failure;
			return false;
		}

		foreach (string s in ErrorBodyMessages) {
			if (doc.Body.TextContent.Contains(s)) {
				sr.Status = SearchResultStatus.IllegalInput;
				return false;
			}

		}

		return true;

	}
}