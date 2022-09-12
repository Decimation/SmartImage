using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Extensions.Configuration;
using SmartImage.Lib.Engines;

namespace SmartImage.Lib;

public class SearchClient
{
	public SearchConfig Config { get; }

	public SearchClient(SearchConfig cfg)
	{
		Config = cfg;
	}

	static SearchClient()
	{
		
	}

	public async Task<IEnumerable<SearchResult>> RunSearchAsync(SearchQuery q, CancellationToken? t = null)
	{
		t ??= CancellationToken.None;

		var e = BaseSearchEngine.All.Where(e => Config.Engines.HasFlag(e.EngineOption))
		                        .Select(e => e.GetResultAsync(q))
		                        .ToArray();

		return await Task.WhenAll(e);
	}
}