global using ICBN = JetBrains.Annotations.ItemCanBeNullAttribute;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Testing;
using Kantan.Net;
using Kantan.Net.Utilities;
using Kantan.Text;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;
using Novus.FileTypes;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Impl.Search;
using SmartImage.Lib.Model;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SmartImage.Lib;

public sealed class SearchClient : IDisposable
{
	public SearchConfig Config { get; init; }

	public bool IsComplete { get; private set; }

	public BaseSearchEngine[] Engines { get; private set; }

	public bool ConfigApplied { get; private set; }

	private static readonly ILogger Logger = LogUtil.Factory.CreateLogger(nameof(SearchClient));

	public SearchClient(SearchConfig cfg)
	{
		Config        = cfg;
		ConfigApplied = false;
		LoadEngines();

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

		var handler = new LoggingHttpMessageHandler(Logger)
		{
			InnerHandler = new HttpLoggingHandler(Logger)
			{
				InnerHandler = new HttpClientHandler()
			}
		};

		Client = new FlurlClient(new HttpClient(handler))
			{ };

		Logger.LogInformation("Init");

	}

	public delegate void ResultCompleteCallback(object sender, SearchResult e);

	public delegate void SearchCompleteCallback(object sender, SearchResult[] e);

	public event ResultCompleteCallback OnResult;

	public event SearchCompleteCallback OnComplete;

	public static FlurlClient Client { get; }

	/// <summary>
	/// Runs a search of <paramref name="query"/>.
	/// </summary>
	/// <param name="query">Search query</param>
	/// <param name="token">Cancellation token passed to <see cref="BaseSearchEngine.GetResultAsync"/></param>
	/// <param name="p"><see cref="IProgress{T}"/></param>
	public async Task<SearchResult[]> RunSearchAsync(SearchQuery query, CancellationToken? token = null,
	                                                 [CBN] IProgress<int> p = null)
	{
		if (!ConfigApplied) {
			await ApplyConfigAsync();
		}

		LoadEngines();

		Debug.WriteLine($"Config: {Config} | {Engines.QuickJoin()}");

		token ??= CancellationToken.None;

		var tasks = GetSearchTasks(query, token.Value);

		var results = new SearchResult[tasks.Count];
		int i       = 0;

		while (tasks.Any()) {
			if (token.Value.IsCancellationRequested) {

				Logger.LogWarning("Cancellation requested");
				IsComplete = true;

				return results;
			}

			var task = await Task.WhenAny(tasks);

			var result = await task;

			OnResult?.Invoke(this, result);
			p?.Report(i);

			if (Config.PriorityEngines.HasFlag(result.Engine.EngineOption)) {

				OpenResult(result);
			}

			results[i] = result;
			i++;
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

	private void OpenResult(SearchResult result)
	{
#if DEBUG
#pragma warning disable CA1822
		// ReSharper disable once MemberCanBeMadeStatic.Local        
		Logger.LogDebug("Not opening result {result}", result);
		return;

#pragma warning restore CA1822
#else
		Url url1;

		if (Config.OpenRaw) {
			url1 = result.RawUrl;
		}
		else {
			url1 = result.Best?.Url ?? result.RawUrl;
		}

		Logger.LogInformation("Opening {Url}", url1);

		HttpUtilities.TryOpenUrl(url1);
#endif

	}

	public List<Task<SearchResult>> GetSearchTasks(SearchQuery query, CancellationToken token)
	{
		if (query.Upload is not { }) {
			throw new ArgumentException($"Query was not uploaded", nameof(query));
		}

		var tasks = Engines.Select(e =>
		{
			var res = e.GetResultAsync(query, token);

			return res;
		}).ToList();

		return tasks;
	}

	public async ValueTask ApplyConfigAsync()
	{
		LoadEngines();

		foreach (BaseSearchEngine bse in Engines) {
			if (bse is IConfig cfg) {
				await cfg.ApplyAsync(Config);
			}
		}

		Logger.LogDebug("Loaded engines");
		ConfigApplied = true;
	}

	public BaseSearchEngine[] LoadEngines()
	{
		return Engines = BaseSearchEngine.All.Where(e =>
			       {
				       return Config.SearchEngines.HasFlag(e.EngineOption) && e.EngineOption != default;
			       })
			       .ToArray();
	}

	[CBN]
	public BaseSearchEngine TryGetEngine(SearchEngineOptions o) => Engines.FirstOrDefault(e => e.EngineOption == o);

	public static ValueTask<IReadOnlyList<SearchResultItem>> Filter(IEnumerable<SearchResultItem> sri)
	{

		var sri2 = sri.AsParallel().DistinctBy(e => e.Url).ToList();

		/*Parallel.ForEachAsync(sri2, async (item, token) =>
		{
			var r = await item.Url.AllowAnyHttpStatus().GetAsync();

			if (r.ResponseMessage.IsSuccessStatusCode) { }
		});*/
		
		return ValueTask.FromResult<IReadOnlyList<SearchResultItem>>(sri2);
	}

	public static IReadOnlyList<SearchResultItem> Optimize(IEnumerable<SearchResultItem> sri)
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
			Logger.LogError("{Error}", e.Message);
		}
		finally { }

		return items.AsReadOnly();
	}

	public static async Task<IReadOnlyList<UniSource>> GetDirectImagesAsync(IEnumerable<SearchResultItem> sri)
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

		}

		return di.AsReadOnly();
	}

	public void Dispose()
	{
		foreach (var engine in Engines) {
			engine.Dispose();
		}

		ConfigApplied = false;
		IsComplete    = false;
	}
}