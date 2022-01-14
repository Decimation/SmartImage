using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Kantan.Net;
using SmartImage.Lib.Searching;
using static Kantan.Diagnostics.LogCategories;

namespace SmartImage.Lib.Engines.Search.Base;

/// <summary>
/// Base search engine.
/// </summary>
public abstract class BaseSearchEngine
{
	protected BaseSearchEngine(string baseUrl)
	{
		BaseUrl = baseUrl;

	}

	public string BaseUrl { get; }

	public abstract SearchEngineOptions EngineOption { get; }

	public virtual string Name => EngineOption.ToString();

	public virtual TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(3);

	protected bool FollowRedirects { get; set; } = true;

	public abstract EngineSearchType SearchType { get; }


	public virtual SearchResult GetResult(ImageQuery query, CancellationToken? c= null)
	{
		var sr = new SearchResult(this)
		{
			Origin = GetResultOrigin(query)
		};
		c ??= CancellationToken.None;
		
		if (c is { IsCancellationRequested: true}) {
			return sr;
		}


		if (!sr.Origin.Success) {
			sr.Status       = ResultStatus.Failure;
			sr.ErrorMessage = $"{sr} | {sr.Origin.Response.StatusCode}";
		}
		else {
			sr.RawUri = sr.Origin.RawUri;
			sr.Status = ResultStatus.Success;
		}


		return sr;
	}


	public async Task<SearchResult> GetResultAsync(ImageQuery query, CancellationToken? c = null)
	{
		
		var task = Task.Run(delegate
		{
			Debug.WriteLine($"{Name}: getting result async", C_VERBOSE);

			var res = GetResult(query, c);

			Debug.WriteLine($"{Name}: result done", C_SUCCESS);

			return res;
		}, c ?? CancellationToken.None);

		return await task;
	}

	protected virtual Uri GetRawUri(ImageQuery query)
	{
		//
		return new(BaseUrl + query.UploadUri);
	}

	protected virtual SearchResultOrigin GetResultOrigin(ImageQuery query, CancellationToken? c = null)
	{
		Uri rawUri = GetRawUri(query);
		

		var res = HttpUtilities.GetHttpResponse(rawUri.ToString(),
		                                             (int) Timeout.TotalMilliseconds,
		                                             HttpMethod.Get, FollowRedirects);
		


		bool success;

		if (res is { IsSuccessStatusCode: false }) {
			if (res.StatusCode == HttpStatusCode.Redirect) {
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

		string content = null;

		if (success && res is { }) {
			var task = res.Content.ReadAsStringAsync();
			
			task.Wait(Timeout);
			content = task.Result;
		}

		var origin = new SearchResultOrigin
		{
			Response = res,
			Content         = content,
			Success  = success,
			RawUri          = rawUri,
			Query           = query
		};

		return origin;

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