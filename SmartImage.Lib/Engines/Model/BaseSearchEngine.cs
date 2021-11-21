using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Kantan.Diagnostics;
using Kantan.Net;
using RestSharp;
using SmartImage.Lib.Searching;
using static Kantan.Diagnostics.LogCategories;

namespace SmartImage.Lib.Engines.Model
{
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


		public virtual SearchResult GetResult(ImageQuery query)
		{
			var sr = new SearchResult(this)
			{
				Origin = GetResultOrigin(query)
			};
			

			if (!sr.Origin.InitialSuccess) {
				sr.Status       = ResultStatus.Unavailable;
				sr.ErrorMessage = $"{sr.Origin.InitialResponse.ErrorMessage} | {sr.Origin.InitialResponse.StatusCode}";
			}
			else {
				sr.RawUri = sr.Origin.RawUri;
				sr.Status = ResultStatus.Success;
			}


			return sr;
		}

		public async Task<SearchResult> GetResultAsync(ImageQuery query)
		{
			var task = Task.Run(delegate
			{
				Debug.WriteLine($"{Name}: getting result async", C_VERBOSE);

				var res = GetResult(query);

				Debug.WriteLine($"{Name}: result done", C_SUCCESS);

				return res;
			});

			return await task;
		}

		protected virtual Uri GetRawUri(ImageQuery query)
		{
			//
			return new(BaseUrl + query.UploadUri);
		}

		protected virtual SearchResultOrigin GetResultOrigin(ImageQuery query)
		{

			Uri rawUri = GetRawUri(query);


			var res = HttpUtilities.GetResponse(rawUri.ToString(), 
			                                    (int) Timeout.TotalMilliseconds, Method.GET, FollowRedirects);


			bool success;

			if (!res.IsSuccessful) {
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

			var origin = new SearchResultOrigin
			{
				InitialResponse = res,
				InitialSuccess  = success,
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
}