using SmartImage.Lib.Engines.Model;

namespace SmartImage.Lib.Engines.Impl.Other
{
	public sealed class BingEngine : BaseSearchEngine
	{
		public BingEngine() : base("https://www.bing.com/images/searchbyimage?cbir=sbi&imgurl=") { }

		public override SearchEngineOptions EngineOption => SearchEngineOptions.Bing;
		

		// Parsing does not seem feasible ATM

		
	}
}