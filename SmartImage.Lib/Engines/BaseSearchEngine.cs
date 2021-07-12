using SimpleCore.Net;
using SmartImage.Lib.Searching;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
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
			var rawUrl = GetRawResultUri(query);

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

		public Uri GetRawResultUri(ImageQuery query)
		{
			var uri = new Uri(BaseUrl + query.UploadUri);

			var reply = Network.Ping(uri, (long)Timeout.TotalMilliseconds);

			//var b = Network.IsAlive(uri, (long) Timeout.TotalMilliseconds);

			//var b1 = ok.Status != IPStatus.Success || ok.Status == IPStatus.TimedOut;

			
			
			if (reply.Status != IPStatus.Success) {
				Debug.WriteLine($"{Name} is unavailable or timed out after {Timeout:g} ({reply.Status})", C_WARN);
				return null;
			}
			return uri;
		}
	}
}