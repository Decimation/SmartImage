#region

using System.Drawing;
using SmartImage.Searching.Model;

#endregion

namespace SmartImage.Searching.Engines.Other
{
	public sealed class KarmaDecayClient : BasicSearchEngine
	{
		public KarmaDecayClient() : base("http://karmadecay.com/search/?q=") { }

		public override string Name => "KarmaDecay";
		public override Color Color => Color.Orange;

		public override SearchEngineOptions Engine => SearchEngineOptions.KarmaDecay;
	}
}