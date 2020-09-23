#region

#endregion

using System;

namespace SmartImage.Searching
{
	public abstract class SimpleSearchEngine : ISearchEngine
	{
		protected readonly string BaseUrl;

		protected SimpleSearchEngine(string baseUrl)
		{
			BaseUrl = baseUrl;
		}

		

		public abstract SearchEngines Engine { get; }

		public abstract string Name { get; }

		public abstract ConsoleColor Color { get; }

		public virtual SearchResult GetResult(string url)
		{
			string rawUrl = GetRawResultUrl(url);
			
			return new SearchResult(this, rawUrl);
		}

		public virtual string GetRawResultUrl(string url)
		{
			return BaseUrl + url;
		}
	}
}