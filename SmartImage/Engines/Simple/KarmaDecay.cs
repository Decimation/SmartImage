#region

using SmartImage.Searching;

#endregion

namespace SmartImage.Engines.Simple
{
	public sealed class KarmaDecay : SimpleSearchEngine
	{
		public KarmaDecay() : base("http://karmadecay.com/search/?q=") { }

		public override string Name => "KarmaDecay";

		public override SearchEngines Engine => SearchEngines.KarmaDecay;
	}
}