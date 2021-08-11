using System;
using System.Diagnostics;
using System.Net;
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

		protected SearchResult GetResult(ImageQuery query, out IRestResponse response)
		{
			var sr = new SearchResult(this);

			if (!GetInitialResult(query, out var rawUrl, out response)) {
				sr.Status       = ResultStatus.Unavailable;
				sr.ErrorMessage = $"{response.ErrorMessage} | {response.StatusCode}";
			}
			else {
				sr.RawUri = rawUrl;
				sr.Status = ResultStatus.Success;
			}

			return sr;
		}

		public virtual SearchResult GetResult(ImageQuery query) => GetResult(query, out _);

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

		protected virtual Uri GetRaw(ImageQuery query)
		{
			//
			return new(BaseUrl + query.UploadUri);

		}

		protected virtual bool GetInitialResult(ImageQuery query, out Uri rawUri, out IRestResponse res)
		{

			rawUri = GetRaw(query);

			/*if (!Network.IsAlive(uri, (int) Timeout.TotalMilliseconds)) {
				Debug.WriteLine($"{Name} is unavailable or timed out after {Timeout:g} | {uri}", C_WARN);
				return null;
			}*/

			res = Network.GetResponse(rawUri.ToString(), (int) Timeout.TotalMilliseconds, Method.GET, FollowRedirects);

			if (!res.IsSuccessful) {
				if ((FollowRedirects && res.StatusCode == HttpStatusCode.Redirect)) {
					return true;
				}

				Debug.WriteLine($"{Name} is unavailable or timed out after " +
				                $"{Timeout:g} | {rawUri} {res.StatusCode}", C_WARN);
				return false;
			}

			return true;
		}
	}
}