// Author: Deci | Project: SmartImage.Lib | Name: BaseSearchEngine.cs
// Date: 2024/06/06 @ 14:06:00

global using R1 = SmartImage.Lib.Resources;
global using Url = Flurl.Url;
using System.Diagnostics;
using Flurl.Http;
using Kantan.Diagnostics;
using Kantan.Net.Utilities;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Engines.Impl.Search;
using SmartImage.Lib.Engines.Impl.Search.Other;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities.Diagnostics;

namespace SmartImage.Lib.Engines;
#nullable enable
public abstract class BaseSearchEngine : IDisposable, IEquatable<BaseSearchEngine>
{

	static BaseSearchEngine()
	{
		Client = (FlurlClient) FlurlHttp.Clients.GetOrAdd(nameof(BaseSearchEngine), null, builder =>
		{
			builder.Headers.AddOrReplace("User-Agent", HttpUtilities.UserAgent);

			// builder.Settings.JsonSerializer = new DefaultJsonSerializer();

			builder.Settings.AllowedHttpStatusRange = "*";

			builder.OnError(f =>
			{
				Debugger.Break();
				Logger.LogError(f.Exception, $"from {f.Request}");
			});

			builder.AddMiddleware(() => new HttpLoggingHandler(Logger));

		});
		;
	}

	protected BaseSearchEngine(string baseUrl, string? endpoint = null)
	{
		BaseUrl     = baseUrl;
		EndpointUrl = endpoint;
		MaxSize     = null;
	}

	protected static readonly ILogger Logger = AppSupport.Factory.CreateLogger(nameof(BaseSearchEngine));

	/// <summary>
	///     The corresponding <see cref="SearchEngineOptions" /> of this engine
	/// </summary>
	public abstract SearchEngineOptions EngineOption { get; }

	/// <summary>
	///     Name of this engine
	/// </summary>
	public virtual string Name => EngineOption.ToString();

	public virtual Url BaseUrl { get; }

	public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);

	public string? EndpointUrl { get; }

	protected long? MaxSize { get; set; }

	protected virtual string[] ErrorBodyMessages { get; } = [];

	protected static FlurlClient Client { get; }

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

	}

	public static bool operator ==(BaseSearchEngine? left, BaseSearchEngine? right)
	{
		return Equals(left, right);
	}

	public static bool operator !=(BaseSearchEngine? left, BaseSearchEngine? right)
	{
		return !Equals(left, right);
	}

	public virtual async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken token = default)
	{
		var b = await VerifyQueryAsync(query);

		/*
		if (!b) {
			// throw new SmartImageException($"{query}");
			Debug.WriteLine($"{query} : Verification error", LogCategories.C_ERROR);
		}
		*/

		var srs = b ? SearchResultStatus.None : SearchResultStatus.IllegalInput;

		var res = new SearchResult(this)
		{
			RawUrl       = GetRawUrl(query),
			ErrorMessage = null,
			Status       = srs
		};

		lock (res.Results) {
			res.Results.Add(res.GetRawResultItem());
		}

		Debug.WriteLine($"{Name} | {query} - {res.Status}", LogCategories.C_INFO);

		return res;
	}

	protected virtual Url GetRawUrl(SearchQuery query)
	{
		//
		Url u = ((BaseUrl + query.Upload));

		return u;
	}

	public virtual ValueTask<bool> VerifyQueryAsync(SearchQuery q)
	{
		bool b = true;

		if (MaxSize.HasValue) {
			b = q.Source.Size <= MaxSize;
		}

		return ValueTask.FromResult(b);
	}

	public int GetHashCode(BaseSearchEngine obj)
	{
		return (int) obj.EngineOption;
	}

	public override string ToString()
	{
		return $"{Name}: {BaseUrl} {Timeout}";
	}

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

	public override int GetHashCode()
	{
		return (int) EngineOption;
	}

	public abstract void Dispose();

	public bool Equals(BaseSearchEngine? other)
	{
		if (other is null) {
			return false;
		}

		if (ReferenceEquals(this, other)) {
			return true;
		}

		return EngineOption == other.EngineOption;
	}

	/*protected IFlurlRequest Build(IFlurlRequest request)
	{
		return request.WithTimeout(Timeout);
	}*/


	// TODO: move config application to ctors?

	/*
	public static readonly BaseSearchEngine[] All =
		ReflectionHelper.CreateAllInAssembly<BaseSearchEngine>(InheritanceProperties.Subclass).ToArray();
		*/

}