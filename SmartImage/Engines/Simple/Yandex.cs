#region

using SmartImage.Searching;

#endregion

namespace SmartImage.Engines.Simple
{
	public sealed class Yandex : SimpleSearchEngine
	{
		public Yandex() : base("https://yandex.com/images/search?rpt=imageview&url=") { }

		public override SearchEngines Engine => SearchEngines.Yandex;

		public override string Name => "Yandex";
	}
}