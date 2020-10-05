#region

using System.Drawing;
using SmartImage.Searching.Model;

#endregion

namespace SmartImage.Searching.Engines.Other
{
	public sealed class BingClient : BasicSearchEngine
	{
		public BingClient() : base("https://www.bing.com/images/searchbyimage?cbir=sbi&imgurl=") { }
		public override SearchEngineOptions Engine => SearchEngineOptions.Bing;
		public override string Name => "Bing";
		public override Color Color => Color.DodgerBlue;
	}
}