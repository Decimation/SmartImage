using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

	public async Task<IEnumerable<SearchResult>> RunSearchAsync(SearchQuery q)
	{
		var e = BaseSearchEngine.All.Where(e => Config.Engines.HasFlag(e.EngineOption))
		                        .Select(e => e.GetResultAsync(q))
		                        .ToArray();

		return await Task.WhenAll(e);
	}
}