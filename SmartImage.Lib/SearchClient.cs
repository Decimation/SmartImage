using System.ComponentModel;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Novus;
using Novus.OS;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Utilities;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Flurl;
using SmartImage.Lib.Cookies;
using SmartImage.Lib.Engines.Upload;
using SmartImage.Lib.Engines.Search.Base;
using SmartImage.Lib.Engines.Upload.Base;
using SmartImage.Lib.Model;

#pragma warning disable CS0162, CS2255
namespace SmartImage.Lib;

public sealed class SearchClient : IDisposable, ISearchConfigReceiver, INotifyPropertyChanged
{

	public SearchConfig Config { get; private set; }

	public bool IsComplete { get; private set; }

	public IEnumerable<BaseSearchEngine> Engines { get; private set; }

	public bool ConfigApplied { get; private set; }

	public bool IsRunning { get; private set; }

	private static readonly ILogger s_logger = AppSupport.Factory.CreateLogger(nameof(SearchClient));

	public SearchClient(SearchConfig cfg)
	{
		Config        = cfg;
		ConfigApplied = false;
		IsRunning     = false;
		Engines       = BaseSearchEngine.GetSelectedEngines(Config.SearchEngines);
	}

	static SearchClient() { }

	public Channel<SearchResult> ResultChannel { get; private set; }

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


	public void OpenChannel()
	{
		var ok = ResultChannel?.Writer.TryComplete();

		ResultChannel = Channel.CreateUnbounded<SearchResult>(new UnboundedChannelOptions()
		{
			SingleWriter = true,
		});

		s_logger.LogInformation("Opened channel | complete: {Ok}", ok);
	}

	/// <summary>
	/// Runs a search of <paramref name="query"/>.
	/// </summary>
	/// <param name="query">Search query</param>
	/// <param name="token">Cancellation token passed to <see cref="BaseSearchEngine.GetResultAsync"/></param>
	public async Task<bool> RunSearchAsync(SearchQuery query, CancellationToken token = default)
	{
		if (ResultChannel == null || (IsComplete && !IsRunning)) {
			// todo: throw?
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

		var tasks   = GetSearchTasks(query, token);
		var results = await Task.WhenAll(tasks);


		s_logger.LogTrace("Results: {Res}", results.Length);

		CompleteSearchAsync();

		if (Config.PriorityEngines == SearchEngineOptions.Auto) {
			var best = GetBest(results);

			if (best != null) {
				s_logger.LogInformation("Best: {Sr}", best.Url);
				OpenResult(best.Url);
			}
		}

		return true;
	}

	/// <inheritdoc />
	public async ValueTask<bool> ApplyConfigAsync(SearchConfig cfg, CancellationToken ct = default)
	{
		Config = cfg;
		s_logger.LogTrace("Loading engines");

		foreach (BaseSearchEngine engine in Engines) {
			if (engine is ISearchConfigReceiver rcvr) {
				s_logger.LogTrace("Applying config to {Engine}", engine.Name);
				await rcvr.ApplyConfigAsync(Config, ct);

			}

			if (engine is ICookiesReceiver ck) {
				s_logger.LogTrace("Applying cookies to {Engine}", engine.Name);
				ck.CookiesSource = Config.GetCookiesSource();
			}
		}

		s_logger.LogDebug("Loaded engines");

		return true;
	}

	[return: MN]
	public static IResultItem GetBest(IEnumerable<SearchResult> results)
	{
		var ordered = results.Select(static x => x.GetBestResult())
		                     .Where(static x => x != null)
		                     .OrderByDescending(static x => x.Similarity);

		var item = ordered.FirstOrDefault();
		return item;
	}

	private void CompleteSearchAsync()
	{
		ResultChannel?.Writer.TryComplete();
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

		// ReSharper disable once HeuristicUnreachableCode
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

		if (Config.PriorityEngines.HasFlag(result.Engine.Option)) {
			var url = Config.OpenRaw ? result.RawUrl : result.GetBestResult()?.Url;

			OpenResult(url);
		}

	}

	public IEnumerable<Task<SearchResult>> GetSearchTasks(SearchQuery query, CancellationToken token)
	{

		return Engines.Select(e =>
		{
			var res = e.GetResultAsync(query, ct: token).ContinueWith((c, tk) =>
			{
				var sr = c.Result;

				ProcessResult(sr);
				return sr;
			}, TaskContinuationOptions.OnlyOnRanToCompletion, token);

			return res;
		});
	}

	public event PropertyChangedEventHandler PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
			return false;

		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}

	public void Dispose()
	{
		s_logger.LogDebug("Disposing {Cfg}", Config);

		foreach (BaseSearchEngine engine in Engines) {
			engine.Dispose();
		}

		Engines = [];


		ConfigApplied = false;
		CompleteSearchAsync();
	}

}