using System.Diagnostics;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Flurl.Http;
using Kantan.Net.Utilities;

namespace SmartImage.Lib.Engines;

public interface IWebContentEngine<T> where T : INode
{
	public async Task<IDocument> ParseDocumentAsync(Url origin, CancellationToken token)
	{
		var parser = new HtmlParser();

		try {
			var res = await origin.AllowAnyHttpStatus()
			                      .WithCookies(out var cj)
			                      // .WithTimeout(Timeout)
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
		catch (FlurlHttpException e) {
			// return await Task.FromException<IDocument>(e);
			Debug.WriteLine($"{this} :: {e.Message}", nameof(ParseDocumentAsync));

			return null;
		}
	}

	public Task<SearchResultItem> ParseResultItemAsync(T n, SearchResult r);

	public Task<IEnumerable<T>> GetItems(IDocument d)
		=> Task.FromResult<IEnumerable<T>>((IEnumerable<T>) d.Body.SelectNodes(NodesSelector));

	public string NodesSelector { get; }
}