using SmartImage.Searching;
using System.Drawing;

namespace SmartImage.Engines.Other
{
	public sealed class BingEngine : BaseSearchEngine
	{
		public BingEngine() : base("https://www.bing.com/images/searchbyimage?cbir=sbi&imgurl=") { }
		public override SearchEngineOptions Engine => SearchEngineOptions.Bing;
		public override string Name => "Bing";
		public override Color Color => Color.DodgerBlue;

		// Parsing does not seem feasible ATM

		public override FullSearchResult GetResult(string url)
		{
			return base.GetResult(url);
		}

		public override string GetRawResultUrl(string url)
		{
			return base.GetRawResultUrl(url);
		}
	}
}