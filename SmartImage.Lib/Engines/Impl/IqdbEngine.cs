

// ReSharper disable UnusedMember.Global

namespace SmartImage.Lib.Engines.Impl
{
	public class IqdbEngine : SearchEngine
	{
		public IqdbEngine() : base("https://iqdb.org/?url=") { }

		public override SearchEngineOptions Engine => SearchEngineOptions.Iqdb;

	}
}
