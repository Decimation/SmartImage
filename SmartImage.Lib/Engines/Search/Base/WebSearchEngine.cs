using AngleSharp.Html.Parser;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib.Engines.Search.Base;

/// <summary>
///     Represents a search engine whose results are from HTML.
/// </summary>
public abstract class WebSearchEngine : ProcessedSearchEngine
{
	protected WebSearchEngine(string baseUrl) : base(baseUrl) { }

	public abstract override SearchEngineOptions EngineOption { get; }

	public abstract override string Name { get; }

	public abstract override EngineSearchType SearchType { get; }

	protected override object ParseContent(SearchResultOrigin s)
	{
		var parser = new HtmlParser();

		var document = parser.ParseDocument(s.Content);

		return document;
	}
}