using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using SimpleCore.Net;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib.Engines
{
	public abstract class BaseSearchEngine
	{
		public string BaseUrl { get; }

		protected BaseSearchEngine(string baseUrl)
		{
			BaseUrl = baseUrl;
		}


		public abstract SearchEngineOptions Engine { get; }

		public virtual string Name => Engine.ToString();

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
			return await Task.Run(delegate
			{
				Debug.WriteLine($"[info] {Name}: getting result async");

				var res = GetResult(query);

				Debug.WriteLine($"[success] {Name}: result done");

				return res;
			});
		}


		public Uri GetRawResultUrl(ImageQuery query)
		{
			var uri = new Uri(BaseUrl + query.Uri);

			bool ok = Network.IsUriAlive(uri);

			if (!ok) {
				Debug.WriteLine($"{uri} is unavailable");
				return null;
			}

			return uri;
		}
	}
}