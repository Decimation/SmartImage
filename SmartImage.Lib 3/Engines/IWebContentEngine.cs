using System.Diagnostics;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Flurl.Http;
using Kantan.Net.Utilities;

namespace SmartImage.Lib.Engines;

public interface IWebContentEngine<T> where T : INode
{

	public async Task<IDocument> ParseDocumentAsync(object origin1, CancellationToken token, SearchQuery q,
	                                                TimeSpan? timeout = null)
	{
		var parser = new HtmlParser();
		timeout ??= Timeout.InfiniteTimeSpan;

		try {
			if (origin1 is Url origin) {
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
			Debug.WriteLine($"{this} :: {e.Message}", nameof(ParseDocumentAsync));

			return null;
		}
	}

	public Task<SearchResultItem> ParseResultItemAsync(T n, SearchResult r);

	public Task<IList<T>> GetItems(IDocument d)
		=> Task.FromResult((IList<T>) d.Body.SelectNodes(NodesSelector));

	public string NodesSelector { get; }
}