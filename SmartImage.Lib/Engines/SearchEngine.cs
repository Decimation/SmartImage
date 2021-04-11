using System;
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
				RawUrl = rawUrl,
			};


			return sr;
		}


		public virtual Uri GetRawResultUrl(ImageQuery query)
		{
			return new Uri(BaseUrl + query.Uri.ToString());
		}
	}
}