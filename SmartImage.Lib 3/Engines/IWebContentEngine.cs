using System.Diagnostics;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Flurl.Http;
using Kantan.Net.Utilities;

namespace SmartImage.Lib.Engines;

public interface IWebContentEngine<TNode> where TNode : INode
{
	public async Task<IDocument> GetDocumentAsync(object origin2, CancellationToken token, SearchQuery query,
	                                              TimeSpan? timeout = null)
	{
		var parser = new HtmlParser();
		timeout ??= Timeout.InfiniteTimeSpan;

		try {
			if (origin2 is Url origin) {
				var res = await origin.AllowAnyHttpStatus()
				                      .WithCookies(out var cj)
				                      .WithTimeout(timeout.Value)
				                      .WithHeaders(new
				                      {
					                      User_Agent = HttpUtilities.UserAgent
				                      })
				                      /*.WithAutoRedirect(true)*/
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

	public Task<SearchResultItem> ParseNodeToItem(TNode n, SearchResult r);

	public Task<IList<TNode>> GetNodes(IDocument d)
		=> Task.FromResult((IList<TNode>) d.Body.SelectNodes(NodesSelector));

	public string NodesSelector { get; }
}