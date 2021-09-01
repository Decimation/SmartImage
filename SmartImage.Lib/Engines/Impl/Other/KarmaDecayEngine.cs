using SmartImage.Lib.Engines.Model;

namespace SmartImage.Lib.Engines.Impl.Other
{
	public sealed class KarmaDecayEngine : BaseSearchEngine
	{
		public KarmaDecayEngine() : base("http://karmadecay.com/search/?q=") { }
		public override EngineResultType ResultType => EngineResultType.External | EngineResultType.Metadata;

		public override SearchEngineOptions EngineOption => SearchEngineOptions.KarmaDecay;
	}
}