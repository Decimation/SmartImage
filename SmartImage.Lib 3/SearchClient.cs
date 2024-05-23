global using ICBN = JetBrains.Annotations.ItemCanBeNullAttribute;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Testing;
using Kantan.Net;
using Kantan.Net.Utilities;
using Kantan.Text;
using Microsoft;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;
using Novus.FileTypes;
using Novus.OS;
using Novus.Win32;
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

	public bool IsRunning { get; private set; }

	private static readonly ILogger Logger = LogUtil.Factory.CreateLogger(nameof(SearchClient));

	internal static readonly Assembly Asm;

	public SearchClient(SearchConfig cfg)
	{
		Config        = cfg;
		ConfigApplied = false;
		IsRunning     = false;
		LoadEngines();
	}

	static SearchClient()
	{
		Asm = Assembly.GetExecutingAssembly();

	}

	[ModuleInitializer]
	public static void Init()
	{
		/*IFlurlClientCache.Configure(settings =>
		{
			settings.Redirects.Enabled                    = true; // default true
			settings.Redirects.AllowSecureToInsecure      = true; // default false
			settings.Redirects.ForwardAuthorizationHeader = true; // default false
			settings.Redirects.MaxAutoRedirects           = 20;   // default 10 (consecutive)

			settings.OnError = r =>
			{
				Debug.WriteLine($"exception: {r.Exception}");
				r.ExceptionHandled = false;

			};
		});*/

		Logger.LogInformation("Init");
	}

	public delegate void ResultCompleteCallback(object sender, SearchResult e);

	public delegate void SearchCompleteCallback(object sender, SearchResult[] e);

	public delegate void ResultOpenCallback(object sender, SearchResultItem e);

	public event ResultCompleteCallback OnResult;

	public event SearchCompleteCallback OnComplete;

	public event ResultOpenCallback OnOpen;

	public Channel<SearchResult> ResultChannel { get; private set; }

	public void OpenChannel()
	{
		ResultChannel?.Writer.TryComplete(new ChannelClosedException("Reopened channel"));

		ResultChannel = Channel.CreateUnbounded<SearchResult>(new UnboundedChannelOptions()
		{
			SingleWriter = true,
		});
	}

	/// <summary>
	/// Runs a search of <paramref name="query"/>.
	/// </summary>
	/// <param name="query">Search query</param>
	/// <param name="reload"></param>
	/// <param name="scheduler"></param>
	/// <param name="token">Cancellation token passed to <see cref="BaseSearchEngine.GetResultAsync"/></param>
	public async Task<SearchResult[]> RunSearchAsync(SearchQuery query, bool reload = true,
	                                                 TaskScheduler scheduler = default,
	                                                 CancellationToken token = default)
	{
		scheduler ??= TaskScheduler.Default;

		// Requires.NotNull(ResultChannel);
		if (ResultChannel == null) {
			OpenChannel();
		}

		if (!query.IsUploaded) {
			throw new ArgumentException($"Query was not uploaded", nameof(query));
		}

		IsRunning = true;

		if (reload) {
			await ApplyConfigAsync();
		}
		else {
			LoadEngines();
		}

		Debug.WriteLine($"Config: {Config} | {Engines.QuickJoin()}");

		List<Task<SearchResult>> tasks = GetSearchTasks(query, scheduler, token);

		var results = new SearchResult[tasks.Count];
		int i       = 0;

		while (tasks.Count > 0) {
			if (token.IsCancellationRequested) {

				Debugger.Break();
				Logger.LogWarning("Cancellation requested");
				ResultChannel?.Writer.Complete();
				IsComplete = true;
				IsRunning  = false;
				return results;
			}

			Task<SearchResult> task = await Task.WhenAny(tasks);
			tasks.Remove(task);

			// Debug.WriteLine($"{task.Id} {task.Status}");
			SearchResult result = await task;

			// ProcessResult(result);

			results[i] = result;
			i++;

			// results.Add(result);
		}

		ResultChannel?.Writer.Complete();
		OnComplete?.Invoke(this, results);
		IsRunning  = false;
		IsComplete = true;

		if (Config.PriorityEngines == SearchEngineOptions.Auto) {

			// var sri    = results.SelectMany(r => r.Results).ToArray();
			// var result = Optimize(sri).FirstOrDefault() ?? sri.FirstOrDefault();
			//todo

			try {

				var ordered = results.Select(x => x.GetBestResult())
					.Where(x => x != null)
					.OrderByDescending(x => x.Similarity);

				var item = ordered.FirstOrDefault();

				OpenResult(item);
			}
			catch (Exception e) {
				Debug.WriteLine($"{e.Message}");

				Debugger.Break();
			}

			/*try {
				IOrderedEnumerable<SearchResultItem> rr = results.SelectMany(rr => rr.Results)
					.OrderByDescending(rr => rr.Score);

				if (Config.OpenRaw) {
					OpenResult(results.MaxBy(x => x.Results.Sum(xy => xy.Score)));
				}
				else {
					OpenResult(rr.OrderByDescending(x => x.Similarity)
						           .FirstOrDefault(x => Url.IsValid(x.Url))?.Url);
				}
			}
			catch (Exception e) {
				Debug.WriteLine($"{e.Message}");

				SearchResult result = results.FirstOrDefault(f => f.Status.IsSuccessful()) ?? results.First();
				OpenResult(result);
			}*/
		}

		IsRunning = false;

		return results;
	}

	private void ProcessResult(SearchResult result)
	{
		OnResult?.Invoke(this, result);

		if (!ResultChannel.Writer.TryWrite(result)) {
			Debug.WriteLine($"Could not write {result}");
		}

		if (Config.PriorityEngines.HasFlag(result.Engine.EngineOption)) {
			OpenResult(result.GetBestResult());
		}
	}

	private static void OpenResult([MN] Url url1)
	{
// #if DEBUG && !TEST
/*
#pragma warning disable CA1822

		// ReSharper disable once MemberCanBeMadeStatic.Local
		Logger.LogDebug("Not opening {url}", url1);
		return;

#pragma warning restore CA1822
*/
// #else

		if (url1 == null) {
			return;
		}

		Logger.LogInformation("Opening {Url}", url1);

		var b = FileSystem.Open(url1, out var proc);

		// var b = Open(url1, out var proc);

		if (b && proc is { }) {
			/*var o = proc.WaitForExit(TimeSpan.FromSeconds(3));

			if (o) {
				Debug.WriteLine($"{proc}");
			}*/
			proc.Dispose();
		}

		// Process.Start(url1);
		// HttpUtilities.TryOpenUrl(url1);
// #endif

	}

	private void OpenResult([MN] SearchResultItem result)
	{
// #if DEBUG && !TEST
/*#pragma warning disable CA1822

		// ReSharper disable once MemberCanBeMadeStatic.Local
		Logger.LogDebug("Not opening result {result}", result);
		return;

#pragma warning restore CA1822*/
// #else

		OnOpen?.Invoke(this, result);

		if (result != null) {

			if (Config.OpenRaw) {
				OpenResult(result.Root.GetRawResultItem().Url);
			}
			else {
				OpenResult(result.Url);
				// OpenResult(result);

			}
		}
// #endif

	}

	public List<Task<SearchResult>> GetSearchTasks(SearchQuery query, TaskScheduler scheduler, CancellationToken token)
	{

		List<Task<SearchResult>> tasks = Engines.Select(e =>
		{
			try {
				Debug.WriteLine($"Starting {e} for {query}");

				Task<SearchResult> res = e.GetResultAsync(query, token: token)
					.ContinueWith((r) =>
					{

						// Debug.Assert(r.IsCompleted);
						// Debug.WriteLine($"{r.Id} :: {r.Status}");
						ProcessResult(r.Result);
						return r.Result;

					}, token, TaskContinuationOptions.None, scheduler);

				return res;
			}
			catch (Exception exception) {
				Debugger.Break();
				Trace.WriteLine($"{exception}");

				// return  Task.FromException(exception);
			}

			return default;
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
	public BaseSearchEngine TryGetEngine(SearchEngineOptions o)
	{
		return Engines.FirstOrDefault(e => e.EngineOption == o);
	}

	/*public static ValueTask<IReadOnlyList<SearchResultItem>> Filter(IEnumerable<SearchResultItem> sri)
	{
		var sri2 = sri.AsParallel().DistinctBy(e => e.Url).ToList();

		return ValueTask.FromResult<IReadOnlyList<SearchResultItem>>(sri2);
	}

	public static IReadOnlyList<SearchResultItem> Optimize(IEnumerable<SearchResultItem> sri)
	{
		var items = sri.Where(r => SearchQuery.IsValidSourceType(r.Url))
			.OrderByDescending(r => r.Score)
			.ThenByDescending(r => r.Similarity)
			.ToArray();

		try {
			/*var c = items.Where(r => r.Root.Engine.EngineOption == SearchEngineOptions.TraceMoe
				/*&& r.Similarity <= TraceMoeEngine.FILTER_THRESHOLD#2#);
			items = items.Except(c).ToArray();#1#

		}
		catch (Exception e) {
			Logger.LogError("{Error}", e.Message);
		}
		finally { }

		return items.AsReadOnly();
	}

	public static async Task<IReadOnlyList<UniSource>> GetDirectImagesAsync(IEnumerable<SearchResultItem> sri)
	{
		//
		var filter = Optimize(sri)
			.DistinctBy(r => r.Url)
			// .Where(r => r.Score >= SearchResultItem.SCORE_THRESHOLD) // probably can be removed/reduced
			.Select(async r =>
			{
				bool b = await r.LoadUniAsync();
				return r.Uni;
			})
			.ToList();

		var di = new List<UniSource>();

		while (filter.Any()) {
			var t1 = await Task.WhenAny(filter);
			filter.Remove(t1);
			var uf = await t1;

			if (uf != null) {
				di.AddRange(uf);
			}

		}

		return di.AsReadOnly();
	}*/

	public void Dispose()
	{
		foreach (BaseSearchEngine engine in Engines) {
			engine.Dispose();
		}

		ConfigApplied = false;
		IsComplete    = false;
		IsRunning     = false;
		ResultChannel.Writer.Complete();
	}

}