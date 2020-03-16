using SmartImage.Model;

namespace SmartImage.Indexers
{
	public sealed class ImgOps : QuickIndexer
	{
		private ImgOps(string baseUrl) : base(baseUrl) { }

		public static ImgOps Value { get; private set; } = new ImgOps("http://imgops.com/");

		public override SearchResult GetResult(string url)
		{
			var rawUrl = GetRawResult(url);
			return new SearchResult(rawUrl, "ImgOps");
		}

		public override OpenOptions Options => OpenOptions.ImgOps;
	}
}