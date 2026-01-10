// Author: Deci | Project: SmartImage.Lib | Name: WebSearchEngine.cs
// Date: 2024/06/06 @ 14:06:00

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using AngleSharp.Dom;
using AngleSharp.XPath;
using Kantan.Diagnostics;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Engines.Results;

namespace SmartImage.Lib.Engines;

/// <summary>
/// Represents a search engine whose results are parsed: <para />
/// <typeparamref name="TSource"/> &#8594; <typeparamref name="TIntermediate"/> &#8594; <typeparamref name="TItem"/>
/// </summary>
public abstract class ParsedSearchEngine<TItem, TIntermediate, TSource> : BaseSearchEngine
	where TItem : SearchResultItem
{

	protected ParsedSearchEngine([NN] Url baseUrl) : base(baseUrl) { }


	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken token = default)
	{
		var res = await base.GetResultAsync(query, token);

		TSource src = default;

		if (res.Status == SearchResultStatus.IllegalInput) {
			goto ret;
		}

		src = await GetSourceAsync(res, query: query, token: token);

		if (!ValidateSource(src)) {
			goto ret;
		}

		var inter   = await ParseIntermediateAsync(src);
		var items = await ParseItemsAsync(inter, res);

		res.Results.AddRange(items);

		// Logger.LogInformation("{Name} :: {RawUrl} source", Name, res.RawUrl);

		res.Status = SearchResultStatus.Success;

	ret:
		res.Update();

		if (src is IDisposable di) {
			di.Dispose();
		}

		// Logger.LogDebug("Disposing {Name} doc", Name);
		return res;
	}

	protected abstract ValueTask<TIntermediate> ParseIntermediateAsync(TSource src);

	protected abstract ValueTask<IEnumerable<TItem>> ParseItemsAsync(TIntermediate source, SearchResult r);

	[ICBN]
	[MURV]
	protected abstract Task<TSource> GetSourceAsync(SearchResult sr, SearchQuery query, CancellationToken token = default);

	protected abstract bool ValidateSource([NNW(true)] TSource src);

}