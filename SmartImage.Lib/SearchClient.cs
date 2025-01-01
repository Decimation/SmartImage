global using EC = System.Runtime.CompilerServices.EnumeratorCancellationAttribute;
global using CMN = System.Runtime.CompilerServices.CallerMemberNameAttribute;
global using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;
global using ICBN = JetBrains.Annotations.ItemCanBeNullAttribute;
global using INN = JetBrains.Annotations.ItemNotNullAttribute;
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
using Kantan.Diagnostics;
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
using SmartImage.Lib.Clients;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Impl.Search;
using SmartImage.Lib.Images;
using SmartImage.Lib.Results;
using SmartImage.Lib.Results.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;
using SmartImage.Lib.Utilities.Diagnostics;
using Kantan.Monad;

namespace SmartImage.Lib;

public sealed class SearchClient : IDisposable
{

	public SearchQuery Query { get; set; }

	public SearchConfig Config { get; init; }

	public bool IsComplete { get; private set; }

	public BaseSearchEngine[] Engines { get; private set; }

	public bool ConfigApplied { get; private set; }

	public bool IsRunning { get; private set; }

	private static readonly ILogger s_logger = AppSupport.Factory.CreateLogger(nameof(SearchClient));

	private static readonly Lock m_lock = new Lock();

	public SearchClient(SearchConfig cfg, SearchQuery query)
	{
		Query         = query;
		Config        = cfg;
		ConfigApplied = false;
		IsRunning     = false;

		Config.PropertyChanged += (sender, args) =>
		{
			if (args.PropertyName == nameof(SearchConfig.SearchEngines)) {
				lock (m_lock) {
					Engines = BaseSearchEngine.GetSelectedEngines(Config.SearchEngines).ToArray();

				}
			}
		};

		// GetSelectedEngines();

	}

	public SearchClient(SearchConfig cfg) : this(cfg, SearchQuery.Null) { }

	static SearchClient() { }

	[ModuleInitializer]
	public static void Init()
	{
		Trace.AutoFlush = true;
		Debug.AutoFlush = true;
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
		});
	}

	public delegate void ResultCompleteCallback(object sender, SearchResult e);

	public delegate void SearchCompleteCallback(object sender, SearchResult[] e);


	public event ResultCompleteCallback OnResultComplete;

	public event SearchCompleteCallback OnSearchComplete;


	public Channel<SearchResult> ResultChannel { get; private set; }

	public void OpenChannel()
	{
		var ok = ResultChannel?.Writer.TryComplete(new ChannelClosedException("Reopened channel"));

		if (ok.HasValue && ok.Value) {
			// ...
		}

		ResultChannel = Channel.CreateBounded<SearchResult>(new BoundedChannelOptions(Engines.Length)
		{
			SingleWriter = true,
		});
	}

	public async IAsyncEnumerable<SearchResult> RunSearchAsync(TaskScheduler scheduler = default,
	                                                           [EC] CancellationToken token = default)
	{
		await RunSearchAsync2(token);

		while (await ResultChannel.Reader.WaitToReadAsync(token)) {
			while (ResultChannel.Reader.TryRead(out var result)) {
				yield return result;
			}
		}
	}

	public ValueTask<bool> RunSearchAsync2(CancellationToken token = default)
		=> RunSearchAsync2(ResultChannel.Writer, token);

	public async ValueTask<bool> RunSearchAsync2(ChannelWriter<SearchResult> cw,
	                                             CancellationToken token = default)
	{
		if (!Query.IsUploaded) {
			throw new SmartImageException($"{Query} was not uploaded");
		}

		IEnumerable<Task<SearchResult>> tasks;

		lock (m_lock) {
			tasks = Engines.Select(e =>
			{
				var task = e.GetResultAsync(Query, token);
				return task;
			});
		}

		await foreach (var task in Task.WhenEach(tasks).WithCancellation(token)) {

			var result = await task;

			if (task.IsFaulted || task.IsCanceled) {
				Trace.WriteLine($"{task} faulted or was canceled");
			}

			if (cw.TryWrite(result)) {
				//
			}

			if (Config.PriorityEngines.HasFlag(result.Engine.EngineOption)) {
				var url = Config.OpenRaw ? result.RawUrl : result.GetBestResult()?.Url;

				OpenResult(url);
			}
		}

		return default;
	}

	public static SearchResultItem GetBest(IEnumerable<SearchResult> results)
	{
		var ordered = results.Select(x => x.GetBestResult())
			.Where(x => x != null)
			.OrderByDescending(x => x.Similarity);

		var item = ordered.FirstOrDefault();
		return item;
	}

	private void CompleteSearchAsync()
	{
		ResultChannel?.Writer.Complete();
		IsRunning  = false;
		IsComplete = true;
	}

	private void ProcessResult(SearchResult result)
	{
		OnResultComplete?.Invoke(this, result);

		if (!ResultChannel.Writer.TryWrite(result)) {
			Debug.WriteLine($"Could not write {result}");
		}

		if (Config.PriorityEngines.HasFlag(result.Engine.EngineOption)) {
			var url = Config.OpenRaw ? result.RawUrl : result.GetBestResult()?.Url;

			OpenResult(url);
		}

	}

	public static void OpenResult([MN] Url url1)
	{
#if (DEBUG && !TEST) || UNITTEST
#pragma warning disable CA1822, CS0162

		// ReSharper disable once MemberCanBeMadeStatic.Local
		s_logger.LogDebug("Not opening result {result}", url1);
		return;

#pragma warning restore CS0162, CA1822
#endif

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


	public IEnumerable<Task<SearchResult>> GetSearchTasks(SearchQuery query, TaskScheduler scheduler,
	                                                      CancellationToken token)
	{
		var tasks = Engines.Select(e =>
		{
			/*try {
				Debug.WriteLine($"Starting {e} for {query}");



				return res;
			}
			catch (Exception exception) {
				Debugger.Break();
				Trace.WriteLine($"{exception}");

				// return  Task.FromException(exception);
			}

			return default;*/

			/*Task<SearchResult> res = e.GetResultAsync(query, token: token)
				.ContinueWith((r) =>
				{
					ProcessResult(r.Result);
					return r.Result;

				}, token, TaskContinuationOptions.None, scheduler)*/
			;

			Task<SearchResult> res = e.GetResultAsync(query, token: token);
			return res;
		});

		return tasks;
	}

	public async ValueTask LoadEnginesAsync(CancellationToken token = default)
	{
		// todo

		Trace.WriteLine("Loading engines");


		if (Config.ReadCookies) {

			await InitCookiesProvider();
		}

		if (Config.FlareSolverr && !FlareSolverrClient.Value.IsInitialized) {


			await InitFlareSolverr();
		}

		foreach (BaseSearchEngine bse in Engines) {
			if (bse is ISearchConfigReceiver cfg) {
				await cfg.ApplyConfigAsync(Config);
			}

			if (Config.ReadCookies && bse is ICookiesReceiver ce) {

				var ok = await ce.ApplyCookiesAsync(DefaultCookiesProvider.Instance, token);

				// if (await CookiesManager.Instance.LoadCookiesAsync()) { }
			}
		}

		// CookiesManager.Instance.Dispose();

		s_logger.LogDebug("Loaded engines");
		ConfigApplied = true;
	}

	private async Task InitFlareSolverr()
	{
		var ok = FlareSolverrClient.Value.Configure(Config.FlareSolverrApiUrl);

		if (!ok) {
			Debugger.Break();
		}
		else {
			// Ensure FlareSolverr

			try {
				var idx = await FlareSolverrClient.Value.Clearance.Solverr.GetIndexAsync();
			}
			catch (Exception e) {
				Trace.WriteLine($"{nameof(FlareSolverrClient)}: {e.Message}");
				Config.FlareSolverr = false;
				FlareSolverrClient.Value.Dispose();
			}
		}
	}

	private async Task InitCookiesProvider()
	{
		try {
			await ((DefaultCookiesProvider) DefaultCookiesProvider.Instance).OpenAsync();
		}
		catch (Exception e) {
			Trace.WriteLine($"{e}");
			Config.ReadCookies = false;
			DefaultCookiesProvider.Instance.Dispose();
		}
	}

	public void Dispose()
	{
		Debug.WriteLine($"Disposing {nameof(SearchClient)}");

		foreach (BaseSearchEngine engine in Engines) {
			engine.Dispose();
		}

		ConfigApplied = false;
		IsComplete    = false;
		IsRunning     = false;
		ResultChannel?.Writer.Complete();
	}

}