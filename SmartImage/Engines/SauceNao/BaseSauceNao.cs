#region

using SmartImage.Model;
using SmartImage.Searching;

#endregion

namespace SmartImage.Engines.SauceNao
{
	public abstract class BaseSauceNao : ISearchEngine
	{
		protected const string BASE_URL = "https://saucenao.com/";

		public string Name => "SauceNao";

		public SearchEngines Engine => SearchEngines.SauceNao;

		public abstract SearchResult GetResult(string url);
	}
}