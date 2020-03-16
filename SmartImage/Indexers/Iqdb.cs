using SmartImage.Model;

namespace SmartImage.Indexers
{
	public class Iqdb : QuickIndexer
	{
		public Iqdb(string baseUrl) : base(baseUrl) { }

		public static Iqdb Value { get; private set; } = new Iqdb("https://iqdb.org/?url=");
		
		public override SearchResult GetResult(string url)
		{
			var rawUrl = GetRawResult(url);
			return new SearchResult(rawUrl, "IQDB");
		}

		public override OpenOptions Options => OpenOptions.Iqdb;
	}
}