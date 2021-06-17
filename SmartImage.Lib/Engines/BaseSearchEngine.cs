using SimpleCore.Net;
using SmartImage.Lib.Searching;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SmartImage.Lib.Utilities;
using static SimpleCore.Diagnostics.LogCategories;

namespace SmartImage.Lib.Engines
{
	/// <summary>
	/// Base search engine.
	/// </summary>
	public abstract class BaseSearchEngine
	{
		public string BaseUrl { get; }

		protected BaseSearchEngine(string baseUrl)
		{
			BaseUrl = baseUrl;
			Timeout = TimeSpan.FromSeconds(5);
		}

		public abstract SearchEngineOptions EngineOption { get; }


		public virtual string Name => EngineOption.ToString();


		public virtual SearchResult GetResult(ImageQuery query)
		{
			var rawUrl = GetRawResultUrl(query);

			var sr = new SearchResult(this);

			if (rawUrl == null) {
				sr.Status = ResultStatus.Unavailable;
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

		public TimeSpan Timeout { get; set; }

		public Uri GetRawResultUrl(ImageQuery query)
		{
			var uri = new Uri(BaseUrl + query.Image);

			//var  hostUri = Network.GetHostUri(new Uri(BaseUrl));

			bool ok      = Network.IsUriAlive(uri, Timeout);

			if (!ok) {
				Debug.WriteLine($"{uri} is unavailable", C_WARN);
				return null;
			}

			return uri;
		}
	}
}