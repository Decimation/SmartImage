using SmartImage.Model;
using SmartImage.Searching;

namespace SmartImage.Engines
{
	public sealed class Bing : QuickSearchEngine
	{
		public Bing() : base("https://www.bing.com/images/searchbyimage?cbir=sbi&imgurl=") { }
		public override SearchEngines Engine => SearchEngines.Bing;
		public override string Name => "Bing";
	}
}