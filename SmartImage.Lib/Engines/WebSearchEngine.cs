// Author: Deci | Project: SmartImage.Lib | Name: WebSearchEngine.cs
// Date: 2025/08/07 @ 03:08:02

using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Flurl.Http;
using Kantan.Net.Utilities;
using SmartImage.Lib.Engines.Results;

namespace SmartImage.Lib.Engines;

public abstract class WebSearchEngine<TItem, TIntermediate> : ParsedSearchEngine<TItem, TIntermediate, IDocument>
	where TItem : SearchResultItem
{

	protected WebSearchEngine(Url url) : base(url) { }

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
				                User_Agent = R1.UserAgent1
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

	/// <inheritdoc />
	protected override bool ValidateSource(IDocument doc)
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