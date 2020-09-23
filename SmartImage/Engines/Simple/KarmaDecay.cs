#region

using System;
using SmartImage.Searching;

#endregion

namespace SmartImage.Engines.Simple
{
	public sealed class KarmaDecay : SimpleSearchEngine
	{
		public KarmaDecay() : base("http://karmadecay.com/search/?q=") { }

		public override string Name => "KarmaDecay";
		public override ConsoleColor Color => ConsoleColor.Yellow;

		public override SearchEngines Engine => SearchEngines.KarmaDecay;
	}
}