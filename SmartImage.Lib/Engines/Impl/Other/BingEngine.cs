namespace SmartImage.Lib.Engines.Impl.Other
{
	public sealed class BingEngine : BaseSearchEngine
	{
		public BingEngine() : base("https://www.bing.com/images/searchbyimage?cbir=sbi&imgurl=") { }

		public override SearchEngineOptions Engine => SearchEngineOptions.Bing;
		

		// Parsing does not seem feasible ATM

		
	}
}