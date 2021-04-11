using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib.Engines
{
	public abstract class SearchEngine
	{
		public string BaseUrl { get; }

		protected SearchEngine(string baseUrl)
		{
			BaseUrl = baseUrl;
		}


		public abstract SearchEngineOptions Engine { get; }

		public virtual string Name => Engine.ToString();

		public virtual SearchResult GetResult(ImageQuery query)
		{
			var rawUrl = GetRawResultUrl(query);


			var sr = new SearchResult(this)
			{
				RawUri = rawUrl,
				Status = ResultStatus.Success
			};


			return sr;
		}

		public async Task<SearchResult> GetResultAsync(ImageQuery query)
		{
			return await Task.Run(delegate
			{
				Debug.WriteLine($"{Name}: getting result async");

				var res = GetResult(query);

				Debug.WriteLine($"{Name}: result done");

				return res;
			});
		}

		public virtual Uri GetRawResultUrl(ImageQuery query)
		{
			var uri = new Uri(BaseUrl + query.Uri.ToString());


			return uri;
		}
	}
}