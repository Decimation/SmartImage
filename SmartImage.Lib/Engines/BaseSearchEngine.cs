// Author: Deci | Project: SmartImage.Lib | Name: BaseSearchEngine.cs
// Date: 2024/06/06 @ 14:06:00


using System.Runtime.CompilerServices;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Engines.Search;
using SmartImage.Lib.Engines.Search.Other;
using SmartImage.Lib.Utilities.Diagnostics;
using SmartImage.Lib.Model;
using SmartImage.Lib.Utilities;
using SmartImage.Shared;

[assembly: InternalsVisibleTo(Common.PROJ_SMARTIMAGE_TEST)]
[assembly: InternalsVisibleTo(Common.PROJ_SMARTIMAGE_UI2)]

namespace SmartImage.Lib.Engines;

#pragma warning disable CA1822
#nullable disable

public abstract class BaseSearchEngine : INamedEnumOption<SearchEngineOptions>, IDisposable, IEquatable<BaseSearchEngine>, IUrl, IMaxLength
{

	protected static readonly ILogger Logger = AppSupport.Factory.CreateLogger(nameof(BaseSearchEngine));

	/// <summary>
	/// Base URI
	/// </summary>

	public virtual Url Url { get; private set; }

	Url IUrl.Url
	{
		get => Url;
	}

	public virtual string Name => Option.ToString();

	public abstract SearchEngineOptions Option { get; }

	[JI]
	public TimeSpan Timeout { get; protected init; }

	[JI]
	public long? MaxLength { get; protected init; }

	[JI]
	protected virtual string[] ErrorBodyMessages { get; }

	protected static IFlurlClient Client {get;}


	static BaseSearchEngine()
	{
		Client = FlurlHttp.Clients.GetOrAdd(nameof(BaseSearchEngine), null, static builder =>
		{
			builder.Headers.AddOrReplace(HeaderNames.UserAgent, R1.UserAgent1);

			// builder.Settings.JsonSerializer = new DefaultJsonSerializer();


			builder.Settings.AllowedHttpStatusRange = "*";
			builder.Settings.HttpVersion            = "2.0";

			builder.OnError(static f =>
			{
				// Debugger.Break();
				Logger.LogError(f.Exception, "Request: {Req}", f.Request);

			});

			// builder.AddMiddleware(static () => new HttpLoggingHandler(_logger));

		});
	}

	protected BaseSearchEngine([NN] Url url)
	{
		Url               = url;
		Timeout           = TimeSpan.FromSeconds(30);
		ErrorBodyMessages = [];
		MaxLength         = null;
		
	}


	public virtual Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken ct = default)
	{
		var b = VerifyQuery(query);

		var srs = b ? SearchResultStatus.None : SearchResultStatus.IllegalInput;

		var res = GetRawResult(query);
		res.Status = srs;

		Logger.LogInformation("{Engine} with {Query} returned {Status}", Name, query, res.Status);

		return Task.FromResult(res);
	}

	protected virtual SearchResult GetRawResult(SearchQuery query)
	{
		var rawUrl = GetRawUrl(query);

		var res = new SearchResult(this, rawUrl) { };

		return res;
	}

	protected virtual Url GetRawUrl(SearchQuery query)
	{
		if (!query.IsUploaded) {
			throw new SmartImageException($"{query} not uploaded");
		}

		Url u = (Url + query.Upload);

		return u;
	}

	public virtual bool VerifyQuery(SearchQuery q)
	{
		bool b = true;

		if (MaxLength.HasValue) {
			b = q.Source.Length <= MaxLength;
		}

		return b;
	}

	/*public int GetHashCode(BaseSearchEngine obj)
	{
		return (int) Option;
	}*/


	public static IEnumerable<BaseSearchEngine> GetSelectedEngines(SearchEngineOptions options)
	{
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

	public override string ToString()
	{
		return $"{Name}: {Url} {Timeout}";
	}


	public override bool Equals([CBN] object obj)
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
		return (int) Option;
	}*/

	public abstract void Dispose();

	public bool Equals([CBN] BaseSearchEngine other)
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

	public static bool operator ==([CBN] BaseSearchEngine left, [CBN] BaseSearchEngine right)
		=> Equals(left, right);

	public static bool operator !=([CBN] BaseSearchEngine left, [CBN] BaseSearchEngine right)
		=> !Equals(left, right);

}