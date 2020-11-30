using System.Drawing;

namespace SmartImage.Engines.Other
{
	public sealed class BingEngine : BasicSearchEngine
	{
		public BingEngine() : base("https://www.bing.com/images/searchbyimage?cbir=sbi&imgurl=") { }
		public override SearchEngineOptions Engine => SearchEngineOptions.Bing;
		public override string Name => "Bing";
		public override Color Color => Color.DodgerBlue;
	}
}