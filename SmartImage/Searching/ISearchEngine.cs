#region

#endregion

namespace SmartImage.Searching
{
	public interface ISearchEngine
	{
		public string Name { get; }

		public SearchEngines Engine { get; }

		public SearchResult GetResult(string url);
	}
}