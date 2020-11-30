using System.Drawing;
using SmartImage.Searching;

namespace SmartImage.Engines
{
	public abstract class BasicSearchEngine : ISearchEngine
	{
		protected readonly string BaseUrl;

		protected BasicSearchEngine(string baseUrl)
		{
			BaseUrl = baseUrl;
		}


		public abstract SearchEngineOptions Engine { get; }

		public abstract string Name { get; }

		public abstract Color Color { get; }

		public virtual FullSearchResult GetResult(string url)
		{
			string rawUrl = GetRawResultUrl(url);

			var sr = new FullSearchResult(this, rawUrl);
			sr.RawUrl = rawUrl;

			return sr;
		}

		public virtual string GetRawResultUrl(string url)
		{
			return BaseUrl + url;
		}
	}
}