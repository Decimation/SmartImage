namespace SmartImage.Model
{
	public interface ISearchEngine
	{
		public SearchEngines Engine { get; }

		public SearchResult GetResult(string url);
	}
}