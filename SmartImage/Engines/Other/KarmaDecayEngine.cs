using System.Drawing;

namespace SmartImage.Engines.Other
{
	public sealed class KarmaDecayEngine : SearchEngine
	{
		public KarmaDecayEngine() : base("http://karmadecay.com/search/?q=") { }

		public override string Name => "KarmaDecay";

		public override Color Color => Color.Orange;

		public override SearchEngineOptions Engine => SearchEngineOptions.KarmaDecay;
	}
}