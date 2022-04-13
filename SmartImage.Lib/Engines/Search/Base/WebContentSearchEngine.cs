using System.Diagnostics;
using AngleSharp.Html.Parser;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib.Engines.Search.Base;

/// <summary>
///     Represents a search engine whose results are parsed from HTML.
/// </summary>
public abstract class WebContentSearchEngine : ProcessedSearchEngine
{
	protected WebContentSearchEngine(string baseUrl) : base(baseUrl) { }

	public abstract override SearchEngineOptions EngineOption { get; }

	public abstract override EngineSearchType SearchType { get; }

	protected override object GetProcessingObject(SearchResult sr) => ParseContent(sr.Origin);

	protected virtual object ParseContent(SearchResultOrigin origin)
	{
		var parser         = new HtmlParser();
		var readStr = origin.Response.Content.ReadAsStringAsync();
		readStr.Wait();
		var content  = readStr.Result;
		var document = parser.ParseDocument(content);

		return document;
	}
}