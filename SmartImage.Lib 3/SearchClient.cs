global using ICBN = JetBrains.Annotations.ItemCanBeNullAttribute;
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

public sealed class SearchClient : IDisposable
{
	public SearchConfig Config { get; init; }

	public bool IsComplete { get; private set; }

	public BaseSearchEngine[] Engines { get; private set; }

	public SearchClient(SearchConfig cfg)
	{
		Config  = cfg;
		Engines = Array.Empty<BaseSearchEngine>();
	}

	static SearchClient() { }

	public delegate void ResultCompleteCallback(object sender, SearchResult e);

	public delegate void SearchCompleteCallback(object sender, List<SearchResult> e);

	public ResultCompleteCallback OnResult { get; set; }

	public SearchCompleteCallback OnComplete { get; set; }

	/// <summary>
	/// Runs a search of <paramref name="query"/>.
	/// </summary>
	/// <param name="query">Search query</param>
	/// <param name="token">Cancellation token passed to <see cref="BaseSearchEngine.GetResultAsync"/></param>
	public async Task<List<SearchResult>> RunSearchAsync(SearchQuery query, CancellationToken? token = null)
	{
		token ??= CancellationToken.None;

		Engines = BaseSearchEngine.All.Where(e => Config.SearchEngines.HasFlag(e.EngineOption)).ToArray();

		var tasks = Engines.Select(e => e.GetResultAsync(query))
		                   .ToList();

		var results = new List<SearchResult>();

		while (tasks.Any()) {

			var task   = await Task.WhenAny(tasks);
			var result = await task;

			OnResult?.Invoke(this, result);

			if (Config.PriorityEngines.HasFlag(result.Engine.EngineOption)) {

				var url1 = result.Best?.Url ?? result.RawUrl;

				if (url1 is { } && Url.IsValid(url1)) {
					HttpUtilities.OpenUrl(url1);
				}

			}

			results.Add(result);
			tasks.Remove(task);
		}

		OnComplete?.Invoke(this, results);

		IsComplete = true;

		if (Config.PriorityEngines == SearchEngineOptions.Auto) {
			/*SearchResult result = results[0];
			double?      max    = 0;

			foreach (SearchResult sr in results) {
				var avg = sr.Results.Average(r => r.Similarity);

				if (avg > max) {
					result = sr;
					max    = avg;
				}
			}

			Debug.WriteLine($"Auto {result}");*/

			try {
				var result = results.SelectMany(r => r.Results)
				                    .Where(r => Url.IsValid(r.Url))
				                    .OrderByDescending(r => r.Similarity)
				                    .First();

				Debug.WriteLine($"{result}");

				HttpUtilities.OpenUrl(result.Url);
			}
			catch (Exception e) { }
		}

		return results;
	}

	#region Implementation of IDisposable

	public void Dispose()
	{
		foreach (var engine in Engines) {
			engine.Dispose();
		}
	}

	#endregion
}