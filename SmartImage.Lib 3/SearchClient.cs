﻿global using ICBN = JetBrains.Annotations.ItemCanBeNullAttribute;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Html;
using Flurl.Http;
using Kantan.Net.Utilities;
using Microsoft.Extensions.Configuration;
using SmartImage.Lib.Engines;

namespace SmartImage.Lib;

public sealed class SearchClient
{
	public SearchConfig Config { get; init; }

	public SearchClient(SearchConfig cfg)
	{
		Config = cfg;
	}

	static SearchClient() { }

	public delegate Task AsyncResultCompleteCallback(object sender, SearchResult e);

	/// <summary>
	/// Runs a search of <paramref name="query"/>.
	/// </summary>
	/// <param name="query">Search query</param>
	/// <param name="token">Cancellation token passed to <see cref="BaseSearchEngine.GetResultAsync"/></param>
	/// <param name="callback">Callback function invoked when a result is returned</param>
	public async Task<List<SearchResult>> RunSearchAsync(SearchQuery query, CancellationToken? token = null,
	                                                     AsyncResultCompleteCallback callback = null)
	{
		token ??= CancellationToken.None;

		var tasks = BaseSearchEngine.All
		                            .Where(e => Config.SearchEngines.HasFlag(e.EngineOption))
		                            .Select(e => e.GetResultAsync(query))
		                            .ToList();

		var results = new List<SearchResult>();

		while (tasks.Any()) {

			var task   = await Task.WhenAny(tasks);
			var result = await task;

			callback?.Invoke(this, result);

			if (Config.PriorityEngines.HasFlag(result.Engine.EngineOption)) {

				var first = result.First;

				if (first is { Url : { } }) {
					HttpUtilities.OpenUrl(first.Url);
				}
			}

			results.Add(result);
			tasks.Remove(task);
		}

		return results;
	}
}