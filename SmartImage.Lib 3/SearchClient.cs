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

public class SearchClient
{
	public SearchConfig Config { get; }

	public SearchClient(SearchConfig cfg)
	{
		Config = cfg;
	}

	static SearchClient() { }

	public delegate Task AsyncEventHandler(object sender, SearchResult e);

	public delegate void AsyncEventHandler2(object sender, SearchResult e);

	public async Task<List<SearchResult>> RunSearchAsync(SearchQuery q, CancellationToken? t = null,
	                                                     AsyncEventHandler ex = null)
	{
		t ??= CancellationToken.None;

		var tasks = (BaseSearchEngine.All
		                         .Where(e => Config.SearchEngines.HasFlag(e.EngineOption))
		                         .Select(e1 =>
		                         {
			                         var task = e1.GetResultAsync(q);

			                         return task;
		                         })).ToList();

		var results = new List<SearchResult>();

		while (tasks.Any()) {

			var task = (await Task.WhenAny(tasks));
			var result  = await task;

			var options = TaskContinuationOptions.AttachedToParent |
			              TaskContinuationOptions.RunContinuationsAsynchronously |
			              TaskContinuationOptions.OnlyOnRanToCompletion;

			ex?.Invoke(this, result);

			if (Config.PriorityEngines.HasFlag(result.Root.EngineOption)) {
				var url = result.Results?.FirstOrDefault(f => f.Url is { })?.Url;

				if (url is { }) {
					HttpUtilities.OpenUrl(url);
				}
			}

			results.Add(result);
			tasks.Remove(task);
		}

		return results;
	}
}