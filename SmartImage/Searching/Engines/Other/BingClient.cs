#region

using System;
using SmartImage.Searching.Model;

#endregion

namespace SmartImage.Searching.Engines.Simple
{
	public sealed class BingClient : BasicSearchEngine
	{
		public BingClient() : base("https://www.bing.com/images/searchbyimage?cbir=sbi&imgurl=") { }
		public override SearchEngines Engine => SearchEngines.Bing;
		public override string Name => "Bing";
		public override ConsoleColor Color => ConsoleColor.Blue;
	}
}