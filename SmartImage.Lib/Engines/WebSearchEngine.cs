// Author: Deci | Project: SmartImage.Lib | Name: WebSearchEngine.cs
// Date: 2025/08/07 @ 03:08:02

using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Flurl.Http;
using Kantan.Net.Utilities;
using SmartImage.Lib.Engines.Results;

namespace SmartImage.Lib.Engines;

public abstract class WebSearchEngine<TResultItem, TIntermediate> : ParsedSearchEngine<TResultItem, TIntermediate, IDocument>
	where TResultItem : SearchResultItem
{

	protected WebSearchEngine(Url baseUrl) : base(baseUrl) { }

	[ICBN]
	[MURV]
	protected override async Task<IDocument> GetSourceAsync(SearchResult sr, SearchQuery query,
	                                                        CancellationToken token = default)
	{

		var parser = new HtmlParser();

		using var res = await Client.Request(sr.RawUrl)
			                .WithCookies(out var cj)
			                .WithTimeout(Timeout)
			                .WithHeaders(new
			                {
				                User_Agent = HttpUtilities.UserAgent
			                })
			                /*.OnError(s =>
			                {
				                s.ExceptionHandled = true;
			                })*/
			                .GetAsync(cancellationToken: token);

		var str = await res.GetStreamAsync();

		var document = await parser.ParseDocumentAsync(str, token);

		return document;
	}

	protected override bool Validate([NNW(true)] IDocument doc, SearchResult sr)
	{
		if (doc is null or { Body: null }) {
			return false;
		}

		foreach (string s in ErrorBodyMessages) {
			if (doc.Body.TextContent.Contains(s)) {
				return false;
			}
		}

		return true;

	}

}