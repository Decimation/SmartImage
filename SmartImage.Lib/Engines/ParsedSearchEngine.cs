// Author: Deci | Project: SmartImage.Lib | Name: WebSearchEngine.cs
// Date: 2024/06/06 @ 14:06:00

using System.Diagnostics;
using SmartImage.Lib.Engines.Results;

namespace SmartImage.Lib.Engines;

/// <summary>
/// Represents a search engine whose results are parsed: <para />
/// <typeparamref name="TSource"/> &#8594; <typeparamref name="TIntermediate"/> &#8594; <typeparamref name="TItem"/>
/// </summary>
public abstract class ParsedSearchEngine<TItem, TIntermediate, TSource> : BaseSearchEngine
	where TItem : IResultItem
{

	protected ParsedSearchEngine([NN] Url url) : base(url) { }


	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken ct = default)
	{
		var res = await base.GetResultAsync(query, ct);

		TSource src = default;

		if (res.Status == SearchResultStatus.IllegalInput) {
			goto ret;
		}

		src = await GetSourceAsync(res, query: query, token: ct);

		if (!ValidateSource(src)) {
			goto ret;
		}

		var inter = await ParseIntermediateAsync(src);
		var items = await ParseItemsAsync(inter, res);

		if (items is IEnumerable<IResultItem> { } items2) {
			res.Results.AddRange(items2);
		}
		else {
			Debugger.Break();
		}


		res.Status = SearchResultStatus.Success;

	ret:
		res.Update();

		if (src is IDisposable di) {
			di.Dispose();
		}

		// _logger.LogDebug("Disposing {Name} doc", Name);
		return res;
	}

	protected abstract ValueTask<TIntermediate> ParseIntermediateAsync(TSource src);

	protected abstract ValueTask<IEnumerable<TItem>> ParseItemsAsync(TIntermediate source, SearchResult r);

	[ICBN]
	[MURV]
	protected abstract Task<TSource> GetSourceAsync(SearchResult sr, SearchQuery query, CancellationToken token = default);

	protected abstract bool ValidateSource([NNW(true)] TSource src);

}