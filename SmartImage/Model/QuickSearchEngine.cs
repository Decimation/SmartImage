namespace SmartImage.Model
{
	public abstract class QuickSearchEngine : ISearchEngine
	{
		protected readonly string BaseUrl;

		protected QuickSearchEngine(string baseUrl)
		{
			BaseUrl = baseUrl;
		}

		public virtual string GetRawResult(string url)
		{
			return BaseUrl + url;
		}

		public abstract SearchResult GetResult(string url);

		public abstract SearchEngines Engine { get; }
	}
}