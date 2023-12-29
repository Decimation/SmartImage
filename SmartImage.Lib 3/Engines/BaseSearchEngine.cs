global using Url = Flurl.Url;
using System.Diagnostics;
using System.Drawing;
using System.Json;
using System.Resources;
using Novus.Utilities;
using SmartImage.Lib.Results;
using AngleSharp.Dom;
using Flurl.Http;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Engines;
#nullable enable

public abstract class BaseSearchEngine : IDisposable
{

	public const int NA_SIZE = -1;

	/// <summary>
	/// The corresponding <see cref="SearchEngineOptions"/> of this engine
	/// </summary>
	public abstract SearchEngineOptions EngineOption { get; }

	/// <summary>
	/// Name of this engine
	/// </summary>
	public virtual string Name => EngineOption.ToString();

	public virtual Url BaseUrl { get; }

	public bool IsAdvanced { get; protected set; }

	public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(3);

	public    TimeSpan Duration { get; protected set; }

	protected long     MaxSize  { get; set; } = NA_SIZE;

	protected virtual string[] ErrorBodyMessages { get; } = Array.Empty<string>();

	protected BaseSearchEngine(string baseUrl)
	{
		BaseUrl    = baseUrl;
		IsAdvanced = true;
	}

	protected static readonly ILogger Logger = LogUtil.Factory.CreateLogger(nameof(BaseSearchEngine));

	protected static FlurlClient Client { get; }

	static BaseSearchEngine()
	{
		var handler = new LoggingHttpMessageHandler(Logger)
		{
			InnerHandler = new HttpLoggingHandler(Logger)
			{
				InnerHandler = new HttpClientHandler()
			}
		};

		BaseSearchEngine.Client = new FlurlClient(new HttpClient(handler))
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
		};
	}

	public override string ToString()
	{
		return $"{Name}: {BaseUrl} {Timeout}";
	}

	protected virtual bool VerifyQuery(SearchQuery q)
	{
		if (q.Upload is not { }) {
			return false;
		}

		bool b, b2;

		if (MaxSize == NA_SIZE || q.Size == NA_SIZE) {
			b = true;
		}

		else {
			b = q.Size <= MaxSize;
		}

		return b;
	}

	protected virtual SearchResultStatus Verify(SearchQuery q)
	{
		var b = VerifyQuery(q);

		return !b ? SearchResultStatus.IllegalInput : SearchResultStatus.None;
	}
	
	public virtual async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken token = default)
	{

		var b = Verify(query);

		/*if (!b) {
			throw new SmartImageException($"{query}");
		}*/

		var res = new SearchResult(this)
		{
			RawUrl       = await GetRawUrlAsync(query),
			Status       = b,
			ErrorMessage = null
		};

		Debug.WriteLine($"{Name} | {query} - {res.Status}", nameof(GetResultAsync));

		return res;
	}

	protected virtual ValueTask<Url> GetRawUrlAsync(SearchQuery query)
	{
		//
		Url u = ((BaseUrl + query.Upload));

		return ValueTask.FromResult(u);
	}

	public abstract void Dispose();

	public static readonly BaseSearchEngine[] All =
		ReflectionHelper.CreateAllInAssembly<BaseSearchEngine>(InheritanceProperties.Subclass).ToArray();

}