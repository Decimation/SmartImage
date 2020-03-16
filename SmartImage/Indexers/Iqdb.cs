using SmartImage.Model;

namespace SmartImage.Indexers
{
	public sealed class Iqdb : QuickSearchEngine
	{
		private Iqdb(string baseUrl) : base(baseUrl) { }

		public static Iqdb Value { get; private set; } = new Iqdb("https://iqdb.org/?url=");
		
		public override SearchResult GetResult(string url)
		{
			var rawUrl = GetRawResult(url);
			return new SearchResult(rawUrl, "IQDB");
		}

		public override SearchEngines Engine => SearchEngines.Iqdb;
	}
}