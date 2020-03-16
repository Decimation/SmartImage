using SmartImage.Model;

namespace SmartImage.Indexers
{
	public sealed class TinEye : QuickIndexer
	{
		private TinEye(string baseUrl) : base(baseUrl) { }
		
		public static TinEye Value { get; private set; } = new TinEye("https://www.tineye.com/search?url=");
		
		public override SearchResult GetResult(string url)
		{
			var rawUrl = GetRawResult(url);
			return new SearchResult(rawUrl, "TinEye");
		}

		public override OpenOptions Options => OpenOptions.TinEye;
	}
}