using SmartImage.Model;

namespace SmartImage.Engines.SauceNao
{
	public sealed class BasicSauceNao : BaseSauceNao
	{
		// QuickSearchEngine
		
		private const string BASIC_RESULT = "https://saucenao.com/search.php?url=";

		public override SearchResult GetResult(string url)
		{
			var u  = BASIC_RESULT + url;
			var sr = new SearchResult(u, Name);
			sr.ExtendedInfo.Add("API not configured");
			return sr;
		}
	}
}