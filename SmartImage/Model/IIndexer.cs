namespace SmartImage.Model
{
	public interface IIndexer
	{
		public OpenOptions Options { get; }

		public string GetRawResult(string url);

		public SearchResult GetResult(string url);
	}
}