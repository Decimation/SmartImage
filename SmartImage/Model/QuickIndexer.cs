namespace SmartImage.Model
{
	public abstract class QuickIndexer
	{
		protected readonly string BaseUrl;

		protected QuickIndexer(string baseUrl)
		{
			BaseUrl = baseUrl;
		}

		public virtual string GetResult(string url)
		{
			return BaseUrl + url;
		}
	}
}