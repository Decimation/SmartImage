global using R1 = SmartImage.Lib.Resources;
global using Url = Flurl.Url;
using System.Diagnostics;
using System.Drawing;
using System.Json;
using System.Resources;
using Novus.Utilities;
using SmartImage.Lib.Results;
using AngleSharp.Dom;
using Flurl.Http;
using Kantan.Diagnostics;
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

	public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);

	public string? EndpointUrl { get; }

	public TimeSpan Duration { get; protected set; }

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
		};
	}

	public override string ToString()
	{
		return $"{Name}: {BaseUrl} {Timeout}";
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
			RawUrl       = await GetRawUrlAsync(query),
			ErrorMessage = null,
			Status = srs
		};

		Debug.WriteLine($"{Name} | {query} - {res.Status}", LogCategories.C_INFO);

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