using SmartImage.Model;

namespace SmartImage.Indexers
{
	public sealed class GoogleImages : QuickIndexer
	{
		private GoogleImages(string baseUrl) : base(baseUrl) { }
		
		public static GoogleImages Value { get; private set; } = new GoogleImages("http://images.google.com/searchbyimage?image_url=");
		
		public override SearchResult GetResult(string url)
		{
			var rawUrl = GetRawResult(url);
			return new SearchResult(rawUrl, "Google Images");
		}

		public override OpenOptions Options => OpenOptions.GoogleImages;
	}
}