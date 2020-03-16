namespace SmartImage.Model
{
	public abstract class QuickIndexer : IIndexer
	{
		protected readonly string BaseUrl;

		protected QuickIndexer(string baseUrl)
		{
			BaseUrl = baseUrl;
		}

		public virtual string GetRawResult(string url)
		{
			return BaseUrl + url;
		}

		public abstract SearchResult GetResult(string url);

		public abstract OpenOptions Options { get; }
	}
}