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

public abstract class ParsedSearchEngine<TResultItem, TIntermediate, TSource> : BaseSearchEngine
	where TResultItem : SearchResultItem
{

	protected ParsedSearchEngine([NN] Url baseUrl) : base(baseUrl) { }


	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken token = default)
	{
		var res = await base.GetResultAsync(query, token);

		TSource doc = default;

		if (res.Status == SearchResultStatus.IllegalInput) {
			goto ret;
		}

		doc = await GetSourceAsync(res, query: query, token: token);

		if (!Validate(doc, res)) {
			goto ret;
		}

		var src   = await ParseIntermediate(doc);
		var items = await ParseResultItems(src, res);

		res.Results.AddRange(items);

		Logger.LogInformation("{Name} :: {RawUrl} source", Name, res.RawUrl);

		res.Status = SearchResultStatus.Success;

	ret:
		res.Update();

		if (doc is IDisposable di) {
			di.Dispose();
		}

		Logger.LogDebug("Disposing {Name} doc", Name);
		return res;
	}

	protected abstract ValueTask<TIntermediate> ParseIntermediate(TSource d);

	protected abstract ValueTask<IEnumerable<TResultItem>> ParseResultItems(TIntermediate source, SearchResult r);

	[ICBN]
	[MURV]
	protected abstract Task<TSource> GetSourceAsync(SearchResult sr, SearchQuery query, CancellationToken token = default);

	protected abstract bool Validate([NNW(true)] TSource doc, SearchResult sr);

}