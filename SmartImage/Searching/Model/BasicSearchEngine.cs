using System.Drawing;

namespace SmartImage.Searching.Model
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

		public virtual SearchResult GetResult(string url)
		{
			string rawUrl = GetRawResultUrl(url);

			var sr = new SearchResult(this, rawUrl);
			sr.RawUrl = rawUrl;

			return sr;
		}

		public virtual string GetRawResultUrl(string url)
		{
			return BaseUrl + url;
		}
	}
}