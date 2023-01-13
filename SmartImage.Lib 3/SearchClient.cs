global using ICBN = JetBrains.Annotations.ItemCanBeNullAttribute;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Html;
using Flurl.Http;
using Kantan.Net.Utilities;
using Microsoft.Extensions.Configuration;
using Novus.FileTypes;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Search;

namespace SmartImage.Lib;

public sealed class SearchClient : IDisposable
{
	public SearchConfig Config { get; init; }

	public bool IsComplete { get; private set; }

	public BaseSearchEngine[] Engines { get; }

	public bool EnginesLoaded { get; private set; }

	public SearchClient(SearchConfig cfg)
	{
		Config        = cfg;
		EnginesLoaded = false;

		Engines = BaseSearchEngine.All.Where(e =>
			{
				return Config.SearchEngines.HasFlag(e.EngineOption) && e.EngineOption != default;
			})
			.ToArray();

	}

	static SearchClient()
	{
		FlurlHttp.Configure(settings =>
		{
			settings.Redirects.Enabled                    = true; // default true
			settings.Redirects.AllowSecureToInsecure      = true; // default false
			settings.Redirects.ForwardAuthorizationHeader = true; // default false
			settings.Redirects.MaxAutoRedirects           = 20;   // default 10 (consecutive)
		});
		Client = new FlurlClient();

		Debug.WriteLine($"Init", nameof(SearchClient));

	}

	public delegate void ResultCompleteCallback(object sender, SearchResult e);

	public delegate void SearchCompleteCallback(object sender, SearchResult[] e);

	public ResultCompleteCallback OnResult { get; set; }

	public SearchCompleteCallback OnComplete { get; set; }

	public static FlurlClient Client { get; }

	/// <summary>
	/// Runs a search of <paramref name="query"/>.
	/// </summary>
	/// <param name="query">Search query</param>
	/// <param name="token">Cancellation token passed to <see cref="BaseSearchEngine.GetResultAsync"/></param>
	public async Task<SearchResult[]> RunSearchAsync(SearchQuery query, CancellationToken? token = null)
	{
		if (!EnginesLoaded) {
			await LoadEngines();
		}

		token ??= CancellationToken.None;

		List<Task<SearchResult>> tasks = GetSearchTasks(query, token.Value);

		var results = new SearchResult[tasks.Count];
		int i       = 0;

		while (tasks.Any()) {
			if (token.Value.IsCancellationRequested) {

				Debug.WriteLine($"Cancellation requested", nameof(RunSearchAsync));
				IsComplete = true;

				return results;
			}

			var task = await Task.WhenAny(tasks);

			/*var cont =task.ContinueWith(async (c) =>
			{
				var opt = await SearchClient.GetDirectImagesAsync(c.Result.Results);

				return opt;
			}, TaskContinuationOptions.OnlyOnRanToCompletion);*/

			var result = await task;

			OnResult?.Invoke(this, result);

			if (Config.PriorityEngines.HasFlag(result.Engine.EngineOption)) {

				OpenResult(result);
			}

			results[i++] = result;
			// results.Add(result);
			tasks.Remove(task);
		}

		OnComplete?.Invoke(this, results);

		IsComplete = true;

		if (Config.PriorityEngines == SearchEngineOptions.Auto) {

			// var sri    = results.SelectMany(r => r.Results).ToArray();
			// var result = Optimize(sri).FirstOrDefault() ?? sri.FirstOrDefault();
			//todo
			OpenResult(results.FirstOrDefault());

		}

		return results;
	}

	private static void OpenResult(SearchResult result)
	{
#if DEBUG
		Debug.WriteLine("Not opening result (DEBUG)", nameof(OpenResult));
		return;
#endif
		var url1 = result.Best?.Url ?? result.RawUrl;
		HttpUtilities.TryOpenUrl(url1);
	}

	public List<Task<SearchResult>> GetSearchTasks(SearchQuery query, CancellationToken token)
	{
		if (query.Upload is not { }) {
			throw new ArgumentException($"Query was not uploaded", nameof(query));
		}

		var tasks = Engines.Select(e =>
		{
			return e.GetResultAsync(query, token);
		}).ToList();

		return tasks;
	}

	public async ValueTask LoadEngines()
	{
		foreach (BaseSearchEngine bse in Engines) {
			await bse.LoadAsync(Config);
		}

		Debug.WriteLine($"Loaded engines", nameof(LoadEngines));
		EnginesLoaded = true;
	}

	[CBN]
	public BaseSearchEngine Find(SearchEngineOptions o) => Engines.FirstOrDefault(e => e.EngineOption == o);

	public static IEnumerable<SearchResultItem> Optimize(IEnumerable<SearchResultItem> sri)
	{
		var items = sri.Where(r => SearchQuery.IsValidSourceType(r.Url))
			.OrderByDescending(r => r.Score)
			.ThenByDescending(r => r.Similarity)
			.ToArray();

		try {
			var c = items.Where(r => r.Root.Engine.EngineOption == SearchEngineOptions.TraceMoe
				/*&& r.Similarity <= TraceMoeEngine.FILTER_THRESHOLD*/);
			items = items.Except(c).ToArray();
		}
		catch (Exception e) {
			Debug.WriteLine($"{e.Message}", nameof(Optimize));
		}
		finally { }

		return items;
	}

	public static async Task<List<UniSource>> GetDirectImagesAsync(IEnumerable<SearchResultItem> sri)
	{
		var filter = Optimize(sri)
			.DistinctBy(r => r.Url)
			// .Where(r => r.Score >= SearchResultItem.SCORE_THRESHOLD) // probably can be removed/reduced
			.Select(async r =>
			{
				bool b = await r.GetUniAsync();
				return r.Uni;
			})
			.ToList();

		var di = new List<UniSource>();

		while (filter.Any()) {
			var t1 = await Task.WhenAny(filter);
			filter.Remove(t1);
			var uf = await t1;

			if (uf != null) {
				di.Add(uf);
			}

			/*if (uf is { Stream: { CanSeek: true, Length: >= 1000 << 2 } }) {
				di.Add(uf);
			}*/
		}

		// di = di.OrderByDescending(d => d.Stream.Length).ToList();

		return di;
	}

	public void Dispose()
	{
		foreach (var engine in Engines) {
			engine.Dispose();
		}

		EnginesLoaded = false;
	}
}