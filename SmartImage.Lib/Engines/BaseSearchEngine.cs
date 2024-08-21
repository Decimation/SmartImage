global using R1 = SmartImage.Lib.Resources;
global using Url = Flurl.Url;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Json;
using System.Resources;
using Novus.Utilities;
using SmartImage.Lib.Results;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Flurl.Http;
using Kantan.Diagnostics;
using Kantan.Net.Utilities;
using Kantan.Net.Web;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Engines.Impl.Search;
using SmartImage.Lib.Engines.Impl.Search.Other;
using SmartImage.Lib.Images;
using SmartImage.Lib.Utilities;
using SmartImage.Lib.Model;

namespace SmartImage.Lib.Engines;
#nullable enable
public abstract class BaseSearchEngine : IDisposable, IEquatable<BaseSearchEngine>
{

	protected static FlurlClient Client { get; }

	/// <summary>
	/// The corresponding <see cref="SearchEngineOptions"/> of this engine
	/// </summary>
	public abstract SearchEngineOptions EngineOption { get; }

	/// <summary>
	/// Name of this engine
	/// </summary>
	public virtual string Name => EngineOption.ToString();

	public virtual Url BaseUrl { get; }

	public bool IsAdvanced { get; protected init; }

	public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);

	public string? EndpointUrl { get; }

	protected long? MaxSize { get; set; }

	protected virtual string[] ErrorBodyMessages { get; } = [];

	protected BaseSearchEngine(string baseUrl, string? endpoint = null)
	{
		BaseUrl     = baseUrl;
		IsAdvanced  = true;
		EndpointUrl = endpoint;
		MaxSize     = null;
	}

	protected static readonly ILogger Logger = LogUtil.Factory.CreateLogger(nameof(BaseSearchEngine));
	
	/*protected IFlurlRequest Build(IFlurlRequest request)
	{
		return request.WithTimeout(Timeout);
	}*/

	static BaseSearchEngine()
	{
		/*var handler = new LoggingHttpMessageHandler(Logger)
		{
			InnerHandler = new HttpLoggingHandler(Logger)
			{
				InnerHandler = new HttpClientHandler()
			}
		};

		Client = new FlurlClient(new HttpClient(handler))
		{
			Settings =
			{
				Redirects =
				{
					Enabled                    = true,
					AllowSecureToInsecure      = true,
					ForwardAuthorizationHeader = true,
					MaxAutoRedirects           = 20,
				},
			}
		};*/
		

		Client = (FlurlClient) FlurlHttp.Clients.GetOrAdd(nameof(BaseSearchEngine), null, builder =>
		{
			
			builder.Settings.AllowedHttpStatusRange = "*";
			builder.AddMiddleware(() => new HttpLoggingHandler(BaseSearchEngine.Logger));

		});;
	}

	public override string ToString()
	{
		return $"{Name}: {BaseUrl} {Timeout}";
	}

	public virtual async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken token = default)
	{
		var b = VerifyQuery(query);

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

	public virtual bool VerifyQuery(SearchQuery q)
	{
		/*if (q.Upload is not { }) {
			return false;
		}*/

		bool b = true;

		if (MaxSize.HasValue) {
			b = q.Image.Size <= MaxSize;
		}

		/*if (MaxSize == NA_SIZE || q.Size == NA_SIZE) {
			b = true;
		}

		else {
			b = q.Size <= MaxSize;
		}*/

		return b;
	}

	// TODO: move config application to ctors?

	public abstract void Dispose();

	/*
	public static readonly BaseSearchEngine[] All =
		ReflectionHelper.CreateAllInAssembly<BaseSearchEngine>(InheritanceProperties.Subclass).ToArray();
		*/

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

	#region

	public bool Equals(BaseSearchEngine? other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;

		return EngineOption == other.EngineOption;
	}

	public override bool Equals(object? obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != this.GetType()) return false;

		return Equals((BaseSearchEngine) obj);
	}

	public override int GetHashCode()
	{
		return (int) EngineOption;
	}

	public static bool operator ==(BaseSearchEngine? left, BaseSearchEngine? right)
	{
		return Equals(left, right);
	}

	public static bool operator !=(BaseSearchEngine? left, BaseSearchEngine? right)
	{
		return !Equals(left, right);
	}

	public int GetHashCode(BaseSearchEngine obj)
	{
		return (int) obj.EngineOption;
	}

	#endregion

}