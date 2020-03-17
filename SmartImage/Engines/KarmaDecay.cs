using SmartImage.Model;

namespace SmartImage.Engines
{
	public sealed class KarmaDecay : QuickSearchEngine
	{
		public KarmaDecay() : base("http://karmadecay.com/search/?q=") {}

		public override string Name => "KarmaDecay";

		public override SearchEngines Engine => SearchEngines.KarmaDecay;
	}
}