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
using Kantan.Net.Web;
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
using SmartImage.Lib.Images;
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

	private static readonly ILogger s_logger = LogUtil.Factory.CreateLogger(nameof(SearchClient));

	internal static readonly Assembly Asm;

	public SearchClient(SearchConfig cfg)
	{
		Config        = cfg;
		ConfigApplied = false;
		IsRunning     = false;

		// GetSelectedEngines();
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
		s_logger.LogInformation("Init");


		FlurlHttp.Clients.WithDefaults(b =>
		{
			b.WithSettings(s =>
			{
				s.Redirects.Enabled                    = true;
				s.Redirects.AllowSecureToInsecure      = true;
				s.Redirects.ForwardAuthorizationHeader = true;
				s.Redirects.MaxAutoRedirects           = 20;
			});
			b.AddMiddleware(() => new HttpLoggingHandler(s_logger));

		});
	}

	public delegate void ResultCompleteCallback(object sender, SearchResult e);

	public delegate void SearchCompleteCallback(object sender, SearchResult[] e);


	public event ResultCompleteCallback OnResultComplete;

	public event SearchCompleteCallback OnSearchComplete;


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
	/// <param name="scheduler"></param>
	/// <param name="token">Cancellation token passed to <see cref="BaseSearchEngine.GetResultAsync"/></param>
	public async Task<SearchResult[]> RunSearchAsync(SearchQuery query,
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

		if (!ConfigApplied) {
			await LoadEnginesAsync(); // todo

		}
		else {
			Debug.WriteLine("Not reloading engines");
		}

		Debug.WriteLine($"Config: {Config} | {Engines.QuickJoin()}");

		List<Task<SearchResult>> tasks = GetSearchTasks(query, scheduler, token).ToList();

		var results = new SearchResult[tasks.Count];
		int i       = 0;

		while (tasks.Count > 0) {
			if (token.IsCancellationRequested) {

				Debugger.Break();
				s_logger.LogWarning("Cancellation requested");
				ResultChannel?.Writer.Complete();
				IsComplete = true;
				IsRunning  = false;
				return results;
			}

			Task<SearchResult> task = await Task.WhenAny(tasks);
			tasks.Remove(task);

			SearchResult result = await task;

			results[i] = result;
			i++;
		}

		ResultChannel?.Writer.Complete();
		OnSearchComplete?.Invoke(this, results);
		IsRunning  = false;
		IsComplete = true;

		if (Config.PriorityEngines == SearchEngineOptions.Auto) {

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
		OnResultComplete?.Invoke(this, result);

		if (!ResultChannel.Writer.TryWrite(result)) {
			Debug.WriteLine($"Could not write {result}");
		}

		if (Config.PriorityEngines.HasFlag(result.Engine.EngineOption)) {
			OpenResult(result.GetBestResult());
		}

	}

	public static void OpenResult([MN] Url url1)
	{
		if (url1 == null) {
			return;
		}

		s_logger.LogInformation("Opening {Url}", url1);

		var b = FileSystem.Open(url1, out var proc);

		// var b = Open(url1, out var proc);

		if (b && proc is { }) {
			proc.Dispose();
		}

	}

	public void OpenResult([MN] SearchResultItem result)
	{
#if DEBUG && !TEST
#pragma warning disable CA1822

		// ReSharper disable once MemberCanBeMadeStatic.Local
		s_logger.LogDebug("Not opening result {result}", result);
		return;

#pragma warning restore CA1822
#endif

		if (result != null) {

			var url = Config.OpenRaw ? result.Root.GetRawResultItem().Url : result.Url;
			OpenResult(url);
		}

	}

	public IEnumerable<Task<SearchResult>> GetSearchTasks(SearchQuery query, TaskScheduler scheduler,
	                                                      CancellationToken token)
	{
		var tasks = Engines.Select(e =>
		{
			try {
				Debug.WriteLine($"Starting {e} for {query}");

				Task<SearchResult> res = e.GetResultAsync(query, token: token)
					.ContinueWith((r) =>
					{
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
		});

		return tasks;
	}

	public async ValueTask LoadEnginesAsync()
	{
		Trace.WriteLine("Loading engines");

		Engines = BaseSearchEngine.GetSelectedEngines(Config.SearchEngines).ToArray();

		if (Config.ReadCookies) {
			if (await CookiesManager.Instance.LoadCookiesAsync()) { }
		}

		foreach (BaseSearchEngine bse in Engines) {
			if (bse is IConfig cfg) {
				await cfg.ApplyConfigAsync(Config);
			}

			if (Config.ReadCookies && bse is ICookieEngine ce) {
				await ce.ApplyCookiesAsync(CookiesManager.Instance.Cookies);

				// if (await CookiesManager.Instance.LoadCookiesAsync()) { }
			}
		}

		// CookiesManager.Instance.Dispose();

		s_logger.LogDebug("Loaded engines");
		ConfigApplied = true;
	}

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