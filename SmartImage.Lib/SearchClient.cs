
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
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
using SmartImage.Lib.Cookies;
using SmartImage.Lib.Engines;
using static System.Runtime.InteropServices.JavaScript.JSType;
using SmartImage.Lib.Utilities.Diagnostics;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using SmartImage.Lib.Engines.Results;

#pragma warning disable CS0162, CS2255
namespace SmartImage.Lib;

public sealed class SearchClient : IDisposable
{

	public SearchConfig Config { get; init; }

	public bool IsComplete { get; private set; }

	public BaseSearchEngine[] Engines { get; private set; }

	public bool ConfigApplied { get; private set; }

	public bool IsRunning { get; private set; }

	private static readonly ILogger s_logger = AppSupport.Factory.CreateLogger(nameof(SearchClient));

	public SearchClient(SearchConfig cfg)
	{
		Config        = cfg;
		ConfigApplied = false;
		IsRunning     = false;
		Engines       = BaseSearchEngine.GetSelectedEngines(Config.SearchEngines).ToArray();

		// GetSelectedEngines();

	}

	static SearchClient() { }

	[ModuleInitializer]
	public static void Init()
	{
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

	/*public delegate void ResultCompleteCallback(object sender, SearchResult e);

	public delegate void SearchCompleteCallback(object sender, SearchResult[] e);


	public event ResultCompleteCallback OnResultComplete;

	public event SearchCompleteCallback OnSearchComplete;*/


	public Channel<SearchResult> ResultChannel { get; private set; }

	public void OpenChannel()
	{
		var ok = ResultChannel?.Writer.TryComplete(new ChannelClosedException("Reopened channel"));

		ResultChannel = Channel.CreateUnbounded<SearchResult>(new UnboundedChannelOptions()
		{
			SingleWriter = true,
		});

		if (ok.HasValue && ok.Value) { }

		// throw new InvalidOperationException();
	}

	/// <summary>
	/// Runs a search of <paramref name="query"/>.
	/// </summary>
	/// <param name="query">Search query</param>
	/// <param name="scheduler"></param>
	/// <param name="token">Cancellation token passed to <see cref="ParsedSearchEngine{TResultItem,TSource}.GetResultAsync(SearchQuery,CancellationToken)"/></param>
	public async Task<bool> RunSearchAsync(SearchQuery query,
	                                       TaskScheduler scheduler = null,
	                                       CancellationToken token = default)
	{
		scheduler ??= TaskScheduler.Default;

		// Requires.NotNull(ResultChannel);
		if (ResultChannel == null || (IsComplete && !IsRunning)) {
			// todo: throw
			OpenChannel();
		}

		if (!query.IsUploaded) {
			throw new ArgumentException($"Query was not uploaded", nameof(query));
		}

		IsRunning = true;

		if (!ConfigApplied) {
			await Config.LoadEnginesAsync(Engines, token).ConfigureAwait(false);
			ConfigApplied = true;
		}

		s_logger.LogTrace("{Config} with {Engines}", Config, Engines.QuickJoin());

		var tasks = GetSearchTasks(query, token);

		/*var results = new SearchResult[tasks.Count];
		int i       = 0;*/

		/*var tf=new TaskFactory(token, TaskCreationOptions.LongRunning, TaskContinuationOptions.None, scheduler);
		tf.StartNew(() =>
		{
			while (ResultChannel.Reader.TryRead()) {

				Debugger.Break();
				s_logger.LogWarning("Cancellation requested");
				goto ret;
			}
		})*/

		/*var results = new List<SearchResult>();
		var consumerTask = Task.Run(async () =>
		{
			await foreach (var item in ResultChannel.Reader.ReadAllAsync(token).ConfigureAwait(false))
				results.Add(item);
		}, token);

		await Task.WhenAll(tasks).ConfigureAwait(false);
		await consumerTask.ConfigureAwait(false);*/

		/*var rg = new List<SearchResult>();

		await foreach (var v in Task.WhenEach(tasks).WithCancellation(token))
		{
			if (token.IsCancellationRequested)
			{
				break;
			}

			var result = await v;
			ProcessResult(result);
			rg.Add(result);
		}*/

		var results = await Task.WhenAll(tasks);
		CompleteSearchAsync();


		/*while (tasks.Count > 0) {
			if (token.IsCancellationRequested) {

				Debugger.Break();
				s_logger.LogWarning("Cancellation requested");
				CompleteSearchAsync();
				return results;
			}

			Task<SearchResult> task = await Task.WhenAny(tasks);
			tasks.Remove(task);

			if (task.IsFaulted) {
				Trace.WriteLine($"{task} faulted!", LogCategories.C_ERROR);
			}

			SearchResult result = await task;

			results[i] = result;
			i++;
		}*/


	ret:

		// OnSearchComplete?.Invoke(this, results);

		/*if (Config.PriorityEngines == SearchEngineOptions.Auto) {

		// todo
			try {

				SearchResultItem item = GetBest(results);

				if (item != null) {
					OpenResult(item.Url);
				}
			}
			catch (Exception e) {
				s_logger.LogError(e, "Run search error");

				Debugger.Break();
			}

		}*/

		IsRunning = false;

		// return rg.ToArray();
		// return results;

		return true;
	}

	[return: MN]
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

	public static void OpenResult([MN] Url url1)
	{
#if (DEBUG && !TEST) || UNITTEST
#pragma warning disable CA1822

		// ReSharper disable once MemberCanBeMadeStatic.Local
		s_logger.LogDebug("Not opening result {result}", url1);
		return;

#pragma warning restore CA1822
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


	private void ProcessResult(SearchResult result)
	{
		// OnResultComplete?.Invoke(this, result);

		if (!ResultChannel.Writer.TryWrite(result)) {
			s_logger.LogWarning("Could not write {Result}", result);
		}

		if (Config.PriorityEngines.HasFlag(result.Engine.EngineOption)) {
			var url = Config.OpenRaw ? result.RawUrl : result.GetBestResult()?.Url;

			OpenResult(url);
		}

	}

	public IEnumerable<Task<SearchResult>> GetSearchTasks(SearchQuery query, CancellationToken token)
	{
		/*return Engines.Select(e =>
		{
			return e.GetResultAsync(query, token: token)
				.ContinueWith((r) =>
				{
					ProcessResult(r.Result);
					return r.Result;

				}, token, TaskContinuationOptions.None, scheduler);
		});*/

		return Engines.Select(async e =>
		{
			var res = await e.GetResultAsync(query, token: token).ConfigureAwait(false);
			ProcessResult(res);
			return res;
		});
	}

	public void Dispose()
	{
		s_logger.LogDebug("Disposing {Client}", Config);

		foreach (BaseSearchEngine engine in Engines) {
			engine.Dispose();
		}

		ConfigApplied = false;
		IsComplete    = false;
		IsRunning     = false;
		ResultChannel?.Writer.Complete();
	}

}