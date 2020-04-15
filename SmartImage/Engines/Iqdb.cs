#region

using SmartImage.Model;
using SmartImage.Searching;

#endregion

namespace SmartImage.Engines
{
	public sealed class Iqdb : QuickSearchEngine
	{
		public Iqdb() : base("https://iqdb.org/?url=") { }

		public override string Name => "IQDB";

		public override SearchEngines Engine => SearchEngines.Iqdb;
	}
}