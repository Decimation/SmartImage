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

		protected SearchResult GetResultInternal(ImageQuery query, out SearchResultStub response)
		{
			var sr = new SearchResult(this);

			var stub = GetResultStub(query);

			if (!stub.InitialSuccess) {
				sr.Status       = ResultStatus.Unavailable;
				sr.ErrorMessage = $"{stub.InitialResponse.ErrorMessage} | {stub.InitialResponse.StatusCode}";
			}
			else {
				sr.RawUri = stub.RawUri;
				sr.Status = ResultStatus.Success;
			}

			response = stub;

			return sr;
		}

		public virtual SearchResult GetResult(ImageQuery query) => GetResultInternal(query, out _);

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

		protected virtual SearchResultStub GetResultStub(ImageQuery query)
		{
			// TODO: Refactor to use HttpClient

			var rawUri = GetRawUri(query);

			var now = Stopwatch.GetTimestamp();

			var res = Network.GetResponse(rawUri.ToString(), (int) Timeout.TotalMilliseconds, Method.GET,
			                              FollowRedirects);

			var diff = TimeSpan.FromTicks(Stopwatch.GetTimestamp() - now);

			bool b;

			if (!res.IsSuccessful) {
				if (res.StatusCode == HttpStatusCode.Redirect) {
					b = true;
				}
				else {
					Debug.WriteLine($"{Name} is unavailable or timed out after " +
					                $"{Timeout:g} | {rawUri} {res.StatusCode}", C_WARN);
					b = false;
				}

			}
			else {
				b = true;
			}

			var stub = new SearchResultStub()
			{
				InitialResponse = res, 
				Retrieval = diff, 
				InitialSuccess = b, 
				RawUri = rawUri
			};

			return stub;

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