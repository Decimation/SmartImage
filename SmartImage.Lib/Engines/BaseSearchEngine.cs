// Author: Deci | Project: SmartImage.Lib | Name: BaseSearchEngine.cs
// Date: 2024/06/06 @ 14:06:00


using System.Runtime.CompilerServices;
using Flurl.Http;
using Kantan.Net.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Search;
using SmartImage.Lib.Engines.Search.Other;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Utilities.Diagnostics;
using SmartImage.Lib.Model;
using SmartImage.Lib.Utilities;

[assembly: InternalsVisibleTo(SearchQuery.PROJ_SMARTIMAGE_TEST)]
[assembly: InternalsVisibleTo(SearchQuery.PROJ_SMARTIMAGE_UI2)]

namespace SmartImage.Lib.Engines;

#pragma warning disable CA1822
#nullable disable

public abstract class BaseSearchEngine : ISearchConfigReceiver, IDisposable, IEquatable<BaseSearchEngine>, ISearchEngine
{

	protected static readonly ILogger Logger = AppSupport.Factory.CreateLogger(nameof(BaseSearchEngine));

	static BaseSearchEngine()
	{
		Client = (FlurlClient) FlurlHttp.Clients.GetOrAdd(nameof(BaseSearchEngine), null, static builder =>
		{
			builder.Headers.AddOrReplace(HeaderNames.UserAgent, HttpUtilities.UserAgent);

			// builder.Settings.JsonSerializer = new DefaultJsonSerializer();


			builder.Settings.AllowedHttpStatusRange = "*";
			builder.Settings.HttpVersion            = "2.0";

			builder.OnError(static f =>
			{
				// Debugger.Break();
				Logger.LogError(f.Exception, "Request: {Req}", f.Request);

			});

			builder.AddMiddleware(static () => new HttpLoggingHandler(Logger));

		});
	}

	protected BaseSearchEngine([NN] Url baseUrl)
	{
		BaseUrl = baseUrl;
	}

	/// <summary>
	/// Base URI
	/// </summary>
	public virtual Url BaseUrl { get; }

	public virtual string Name
	{
		get => EngineOption.ToString();
	}

	/// <inheritdoc />
	public abstract SearchEngineOptions EngineOption { get; }

	[JI]
	public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

	[JI]
	protected long? MaxSize { get; init; }

	[JI]
	protected virtual string[] ErrorBodyMessages { get; } = [];

	protected static FlurlClient Client { get; }


	/*public Task<SearchResult> GetTaskAsync(SearchQuery query, CancellationToken token = default)
	{
		// TODO

		Task ops;
		if (this is ISearchQueryVerifiable sq) {
			ops = sq.VerifyQueryAsync(query);
		}

		var task = GetResultAsync(query, token);

	}*/

	public virtual Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken token = default)
	{
		var b = VerifyQuery(query);

		var srs = b ? SearchResultStatus.None : SearchResultStatus.IllegalInput;

		var rawUrl = GetRawUrl(query);

		var res = new SearchResult(this, rawUrl)
		{
			Status = srs,
		};

		Logger.LogInformation("{Engine} with {Query} returned {Status}", Name, query, res.Status);

		return Task.FromResult(res);
	}


	protected virtual Url GetRawUrl(SearchQuery query)
	{
		if (!query.IsUploaded) {
			throw new SmartImageException($"{query} not uploaded");
		}

		Url u = (BaseUrl + query.Upload);

		return u;
	}

	public virtual bool VerifyQuery(SearchQuery q)
	{
		bool b = true;

		if (MaxSize.HasValue) {
			b = q.Source.Size <= MaxSize;
		}

		return b;
	}

	/*public int GetHashCode(BaseSearchEngine obj)
	{
		return (int) EngineOption;
	}*/

	public override string ToString()
	{
		return $"{Name}: {BaseUrl} {Timeout}";
	}

	public abstract ValueTask<bool> ApplyConfigAsync(SearchConfig cfg, CancellationToken ct = default);


	public override bool Equals(object? obj)
	{
		if (obj is null) {
			return false;
		}

		if (ReferenceEquals(this, obj)) {
			return true;
		}

		if (obj.GetType() != GetType()) {
			return false;
		}

		return Equals((BaseSearchEngine) obj);
	}

	/*public override int GetHashCode()
	{
		return (int) EngineOption;
	}*/

	public abstract void Dispose();

	public bool Equals(BaseSearchEngine? other)
	{
		if (other is null) {
			return false;
		}

		if (ReferenceEquals(this, other)) {
			return true;
		}

		return other.Equals(this);
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		return (Name != null ? Name.GetHashCode() : 0);
	}

	public static bool operator ==(BaseSearchEngine? left, BaseSearchEngine? right)
		=> Equals(left, right);

	public static bool operator !=(BaseSearchEngine? left, BaseSearchEngine? right)
		=> !Equals(left, right);


	public static IEnumerable<BaseSearchEngine> GetSelectedEngines(SearchEngineOptions options)
	{
		/*return BaseSearchEngine.All.Where(e =>
			{
				return e.EngineOption != default && options.HasFlag(e.EngineOption);
			})
			.ToArray();*/

		if (options.HasFlag(SearchEngineOptions.SauceNao))
			yield return new SauceNaoEngine();

		if (options.HasFlag(SearchEngineOptions.ImgOps))
			yield return new ImgOpsEngine();

		if (options.HasFlag(SearchEngineOptions.GoogleImages))
			yield return new GoogleImagesEngine();

		if (options.HasFlag(SearchEngineOptions.TinEye))
			yield return new TinEyeEngine();

		if (options.HasFlag(SearchEngineOptions.Iqdb))
			yield return new IqdbEngine();

		if (options.HasFlag(SearchEngineOptions.TraceMoe))
			yield return new TraceMoeEngine();

		if (options.HasFlag(SearchEngineOptions.KarmaDecay))
			yield return new KarmaDecayEngine();

		if (options.HasFlag(SearchEngineOptions.Yandex))
			yield return new YandexEngine();

		if (options.HasFlag(SearchEngineOptions.Bing))
			yield return new BingEngine();

		if (options.HasFlag(SearchEngineOptions.Ascii2D))
			yield return new Ascii2DEngine();

		if (options.HasFlag(SearchEngineOptions.RepostSleuth))
			yield return new RepostSleuthEngine();

		if (options.HasFlag(SearchEngineOptions.EHentai))
			yield return new EHentaiEngine();

		if (options.HasFlag(SearchEngineOptions.ArchiveMoe))
			yield return new ArchiveMoeEngine();

		if (options.HasFlag(SearchEngineOptions.Iqdb3D))
			yield return new Iqdb3DEngine();

		if (options.HasFlag(SearchEngineOptions.Fluffle))
			yield return new FluffleEngine();

		if (options.HasFlag(SearchEngineOptions.GoogleLens))
			yield return new GoogleLensEngine();
	}

}