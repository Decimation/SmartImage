using System.Diagnostics;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Flurl.Http;
using Kantan.Net.Utilities;

namespace SmartImage.Lib.Engines;

public interface IWebContentEngine
{
	public async Task<IDocument> GetDocumentAsync(object origin2, SearchQuery query,
	                                              TimeSpan? timeout = null, 
	                                              CancellationToken? token = null)
	{
		token   ??= CancellationToken.None;
		timeout ??= Timeout.InfiniteTimeSpan;

		var parser = new HtmlParser();

		try {
			if (origin2 is Url origin) {
				var res = await origin.AllowAnyHttpStatus()
				                      .WithCookies(out var cj)
				                      .WithTimeout(timeout.Value)
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

	public Task<SearchResultItem> ParseNodeToItem(INode n, SearchResult r);

	public Task<List<INode>> GetNodes(IDocument d)
		=> Task.FromResult(d.Body.SelectNodes(NodesSelector));

	public string NodesSelector { get; }
}