namespace SmartImage.Model
{
	public abstract class QuickSearchEngine : ISearchEngine
	{
		protected readonly string BaseUrl;

		protected QuickSearchEngine(string baseUrl)
		{
			BaseUrl = baseUrl;
		}
		
		public abstract SearchEngines Engine { get; }
		
		public abstract string Name { get; }

		public virtual string GetRawResult(string url)
		{
			return BaseUrl + url;
		}
		
		public virtual SearchResult GetResult(string url)
		{
			string rawUrl = GetRawResult(url);
			return new SearchResult(rawUrl, Name);
		}
	}
}