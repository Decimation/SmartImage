using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Kantan.Collections;
using Kantan.Net;
using Kantan.Net.Utilities;
using Kantan.Text;
using Novus.Utilities;
using SmartImage.Lib.Properties;
using SmartImage.Lib.Searching;
using static Kantan.Diagnostics.LogCategories;

namespace SmartImage.Lib.Engines.Search.Base;

/// <summary>
/// Base search engine.
/// </summary>
public abstract class BaseSearchEngine : IDisposable
{
	protected BaseSearchEngine(string baseUrl)
	{
		BaseUrl = baseUrl;
		Client  = new HttpClient();

	}

	public string BaseUrl { get; }

	public abstract SearchEngineOptions EngineOption { get; }

	public virtual string Name => EngineOption.ToString();

	public virtual TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(3);

	protected bool FollowRedirects { get; set; } = true;

	public abstract EngineSearchType SearchType { get; }

	protected HttpClient Client { get; }

	public virtual SearchResult GetResult(ImageQuery query, CancellationToken? c = null)
	{
		var sr = new SearchResult(this)
		{
			Origin = GetResultOrigin(query)
		};

		c ??= CancellationToken.None;

		if (c is { IsCancellationRequested: true }) {
			return sr;
		}

		if (!sr.Origin.Success) {
			sr.Status       = SearchResultStatus.Failure;
			sr.ErrorMessage = $"{sr} | {sr.Origin.Response.StatusCode}";
		}
		else {
			sr.RawUri = sr.Origin.RawUri;
			sr.Status = SearchResultStatus.Success;
		}

		return sr;
	}

	public async Task<SearchResult> GetResultAsync(ImageQuery query, CancellationToken? c = null)
	{
		c ??= CancellationToken.None;

		var task = Task.Run(delegate
		{
			Debug.WriteLine($"{Name}: getting result async", C_VERBOSE);

			var res = GetResult(query, c);

			Debug.WriteLine($"{Name}: result done", C_SUCCESS);

			return res;
		}, c.Value);

		return await task;
	}

	protected virtual Uri GetRawUri(ImageQuery query)
	{
		//
		return new(BaseUrl + query.UploadUri);
	}

	protected virtual SearchResultOrigin GetResultOrigin(ImageQuery query, CancellationToken? c = null)
	{
		var rawUri = GetRawUri(query);

		const byte i = 0xFF;

		var res = HttpUtilities.GetHttpResponse(rawUri.ToString(), (int) Timeout.TotalMilliseconds,
		                                        HttpMethod.Get, FollowRedirects, token: c);

		// var task = rawUri.WithClient(new FlurlClient(Client)).WithTimeout(Timeout).WithAutoRedirect(true).GetAsync();
		// task.Wait();
		// var res = task.Result;

		bool success;

		if (res is { ResponseMessage.IsSuccessStatusCode: false }) {
			if (res.ResponseMessage.StatusCode == HttpStatusCode.Redirect) {
				success = true;
			}
			else {
				Debug.WriteLine($"{Name} is unavailable or timed out after " +
				                $"{Timeout:g} | {rawUri} {res.StatusCode}", C_WARN);
				success = false;
			}
		}
		else {
			success = true;
		}

		// string content = null;

		if (success && res is { }) {
			// var task = res.Content.ReadAsStringAsync();
			// task.Wait(Timeout);
			// content = task.Result;
		}

		var origin = new SearchResultOrigin
		{
			Response = res?.ResponseMessage,
			// Content  = content,
			Success = success,
			RawUri  = rawUri,
			Query   = query
		};

		return origin;

	}

	public static BaseSearchEngine[] GetAllSearchEngines()
	{
		var engines = typeof(BaseSearchEngine).GetAllSubclasses()
		                                      .Select(Activator.CreateInstance)
		                                      .Cast<BaseSearchEngine>()
		                                      .ToList();

		for (var i = engines.Count - 1; i >= 0; i--) {
			BaseSearchEngine engine = engines[i];

			var attr = engine.GetType().GetTypeInfo()
			                 .GetCustomAttributes(typeof(ObsoleteAttribute), true)
			                 .Cast<ObsoleteAttribute>()
			                 .FirstOrDefault();

			if (attr is { }) {
				Debug.WriteLine($"Removing obsolete engine: {engine.Name}", C_INFO);
				engines.RemoveAt(i);
			}
		}

		return engines.ToArray();
	}

	public virtual void Dispose()
	{
		Client.Dispose();
		GC.SuppressFinalize(this);
	}
}

/// <summary>
/// Indicates the search criteria and result type of an engine.
/// </summary>
[Flags]
public enum EngineSearchType
{
	/// <summary>
	/// The engine returns image results
	/// </summary>
	Image,

	/// <summary>
	/// The engine returns metadata
	/// </summary>
	Metadata,

	/// <summary>
	/// The engine returns external information
	/// </summary>
	External,

	Other
}