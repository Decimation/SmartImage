#region

using System;
using SmartImage.Searching;

#endregion

namespace SmartImage.Engines.Simple
{
	public sealed class Bing : SimpleSearchEngine
	{
		public Bing() : base("https://www.bing.com/images/searchbyimage?cbir=sbi&imgurl=") { }
		public override SearchEngines Engine => SearchEngines.Bing;
		public override string Name => "Bing";
		public override ConsoleColor Color => ConsoleColor.Cyan;
	}
}