using System;
using System.Diagnostics;
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


		protected SearchResult GetPreliminaryResult(ImageQuery query, out IRestResponse response)
		{
			var rawUrl = GetRawResultUri(query, out response);

			var sr = new SearchResult(this);

			if (rawUrl == null) {
				sr.Status       = ResultStatus.Unavailable;
				sr.ErrorMessage = $"{response.ErrorMessage} | {response.StatusCode}";
			}
			else {
				sr.RawUri = rawUrl;
				sr.Status = ResultStatus.Success;
			}


			return sr;
		}

		public virtual SearchResult GetResult(ImageQuery query)
		{
			return GetPreliminaryResult(query, out _);
		}


		public async Task<SearchResult> GetResultAsync(ImageQuery query)
		{

			var task = Task.Run(delegate
			{
				Debug.WriteLine($"{Name}: getting result async", C_INFO);

				var res = GetResult(query);

				Debug.WriteLine($"{Name}: result done", C_SUCCESS);

				return res;
			});

			return await task;
		}


		protected virtual Uri GetRawResultUri(ImageQuery query, out IRestResponse res)
		{
			var uri = new Uri(BaseUrl + query.UploadUri);

			/*if (!Network.IsAlive(uri, (int) Timeout.TotalMilliseconds)) {
				Debug.WriteLine($"{Name} is unavailable or timed out after {Timeout:g} | {uri}", C_WARN);
				return null;
			}*/

			res = Network.GetResponse(uri.ToString(), (int) Timeout.TotalMilliseconds, Method.GET, true);

			if (!res.IsSuccessful /* && res.StatusCode!= HttpStatusCode.Redirect*/) {
				Debug.WriteLine($"{Name} is unavailable or timed out after " +
				                $"{Timeout:g} | {uri} {res.StatusCode}", C_WARN);
				return null;
			}

			return uri;
		}

		protected static SearchResult TryProcess(SearchResult sr, Func<SearchResult, SearchResult> process)
		{
			if (!sr.IsSuccessful) {
				return sr;
			}

			try {

				sr = process(sr);
			}
			catch (Exception e) {
				sr.Status       = ResultStatus.Failure;
				sr.ErrorMessage = e.Message;
				Trace.WriteLine($"{sr.Engine.Name}: {e.Message}", LogCategories.C_ERROR);
			}

			return sr;
		}
	}
}