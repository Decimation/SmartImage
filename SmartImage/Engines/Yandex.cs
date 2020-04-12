using SmartImage.Model;
using SmartImage.Searching;

namespace SmartImage.Engines
{
	public sealed class Yandex : QuickSearchEngine
	{
		public Yandex() : base("https://yandex.com/images/search?rpt=imageview&url=") { }

		public override SearchEngines Engine => SearchEngines.Yandex;

		public override string Name => "Yandex";
	}
}