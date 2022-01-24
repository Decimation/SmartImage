using AngleSharp.Html.Parser;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib.Engines.Search.Base;

/// <summary>
///     Represents a search engine whose results are from HTML.
/// </summary>
public abstract class WebClientSearchEngine : ProcessedSearchEngine
{
	protected WebClientSearchEngine(string baseUrl) : base(baseUrl) { }

	public abstract override SearchEngineOptions EngineOption { get; }
	
	public abstract override EngineSearchType SearchType { get; }

	protected override object GetProcessingObject(SearchResult r)
	{
		return ParseContent(r.Origin);
	}

	protected virtual object ParseContent(SearchResultOrigin s)
	{
		var    parser            = new HtmlParser();
		var async = s.Response.Content.ReadAsStringAsync();
		async.Wait();
		var content = async.Result;
		var document = parser.ParseDocument((string) content);

		return document;
	}
}