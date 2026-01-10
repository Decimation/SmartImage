using Argon;
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
using Novus;
using Novus.FileTypes;
using Novus.OS;
using Novus.Win32;
using SmartImage.Lib.Clients;
using SmartImage.Lib.Cookies;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Model;
using SmartImage.Lib.Utilities;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using static System.Runtime.InteropServices.JavaScript.JSType;

#pragma warning disable CS0162, CS2255
namespace SmartImage.Lib;

public sealed class SearchClient : IDisposable, ISearchConfigReceiver
{

	public SearchConfig Config { get; private set;}

	public bool IsComplete { get; private set; }

	public IEnumerable<BaseSearchEngine> Engines { get; private set;}

	public bool ConfigApplied { get; private set; }

	public bool IsRunning { get; private set; }

	private static readonly ILogger s_logger = AppSupport.Factory.CreateLogger(nameof(SearchClient));

	public SearchClient(SearchConfig cfg)
	{
		Config        = cfg;
		ConfigApplied = false;
		IsRunning     = false;
		Engines       = Config.GetSelectedEngines();
	}

	static SearchClient() { }

	[ModuleInitializer]
	public static void Init()
	{
		s_logger.LogInformation("Init");
		Global.Setup();

		FlurlHttp.Clients.WithDefaults(static b =>
		{
			b.WithSettings(static s =>
			{
				s.Redirects.Enabled                    = true;
				s.Redirects.AllowSecureToInsecure      = true;
				s.Redirects.ForwardAuthorizationHeader = true;
				s.Redirects.MaxAutoRedirects           = 20;
			});
		});
	}


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
	/// <param name="token">Cancellation token passed to <see cref="BaseSearchEngine.GetResultAsync"/></param>
	public async Task<bool> RunSearchAsync(SearchQuery query, CancellationToken token = default)
	{
		if (ResultChannel == null || (IsComplete && !IsRunning)) {
			// todo: throw
			OpenChannel();
		}

		if (!query.IsUploaded) {
			throw new ArgumentException($"Query was not uploaded", nameof(query));
		}

		IsRunning = true;

		if (!ConfigApplied) {
			await ApplyConfigAsync(Config, token);
			ConfigApplied = true;

		}
		var tasks = GetSearchTasks(query, token);

		var results = await Task.WhenAll(tasks);

		s_logger.LogTrace("Results: {Res}", results.Length);

		CompleteSearchAsync();

		return true;
	}

	/// <inheritdoc />
	public async ValueTask<bool> ApplyConfigAsync(SearchConfig cfg, CancellationToken ct = default)
	{
		Config = cfg;
		Engines = Config.GetSelectedEngines();

		s_logger.LogTrace("Loading engines");

		foreach (var engine in Engines) {

			if (engine is ISearchConfigReceiver rcvr) {
				s_logger.LogTrace("Applying config to {Engine}", engine.Name);
				await rcvr.ApplyConfigAsync(Config, ct);

			}

			if (engine is ICookiesReceiver ck) {
				s_logger.LogTrace("Applying cookies to {Engine}", engine.Name);
				await ck.ApplyCookiesAsync(Config.GetCookiesSource(), ct);
			}
		}

		// CookiesManager.Instance.Dispose();

		s_logger.LogDebug("Loaded engines");

		// ConfigApplied = true;

		return true;
	}

	[return: MN]
	public static SearchResultItem GetBest(IEnumerable<SearchResult> results)
	{
		var ordered = results.Select(static x => x.GetBestResult())
			.Where(static x => x != null)
			.OrderByDescending(static x => x.Similarity);

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

		return Engines.Select(e =>
		{
			var res = e.GetResultAsync(query, token: token).ContinueWith(c =>
			{
				var sr = c.Result;
				ProcessResult(sr);
				return sr;
			}, TaskContinuationOptions.OnlyOnRanToCompletion);

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
		CompleteSearchAsync();
	}

}