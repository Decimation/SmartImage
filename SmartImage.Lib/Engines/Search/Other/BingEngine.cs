using SmartImage.Lib.Engines.Search.Base;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib.Engines.Search.Other;

public sealed class BingEngine : BaseSearchEngine
{
	public BingEngine() : base("https://www.bing.com/images/searchbyimage?cbir=sbi&imgurl=") { }

	public override SearchEngineOptions EngineOption => SearchEngineOptions.Bing;

	public override EngineSearchType SearchType => EngineSearchType.Image;


	// Parsing does not seem feasible ATM
}