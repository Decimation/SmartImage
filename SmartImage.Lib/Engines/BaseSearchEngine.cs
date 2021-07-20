using Kantan.Net;
using SmartImage.Lib.Searching;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Kantan.Diagnostics;
using RestSharp;
using SmartImage.Lib.Utilities;
using static Kantan.Diagnostics.LogCategories;

namespace SmartImage.Lib.Engines
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

		public virtual SearchResult GetResult(ImageQuery query)
		{
			var rawUrl = GetRawResultUri(query, out var r);

			var sr = new SearchResult(this);

			if (rawUrl == null) {
				sr.Status       = ResultStatus.Unavailable;
				sr.ErrorMessage = $"{r.ErrorMessage} | {r.StatusCode}";

			}
			else {
				sr.RawUri = rawUrl;
				sr.Status = ResultStatus.Success;
			}


			return sr;
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



		public virtual Uri GetRawResultUri(ImageQuery query, out IRestResponse res)
		{
			var uri = new Uri(BaseUrl + query.UploadUri);

			/*if (!Network.IsAlive(uri, (int) Timeout.TotalMilliseconds)) {
				Debug.WriteLine($"{Name} is unavailable or timed out after {Timeout:g} | {uri}", C_WARN);
				return null;
			}*/

			res = Network.GetResponse(uri.ToString(), (int) Timeout.TotalMilliseconds, Method.GET, true);

			if (!res.IsSuccessful && res.StatusCode!= HttpStatusCode.Redirect) {
				Debug.WriteLine($"{Name} is unavailable or timed out after {Timeout:g} | {uri} {res.StatusCode}", C_WARN);
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