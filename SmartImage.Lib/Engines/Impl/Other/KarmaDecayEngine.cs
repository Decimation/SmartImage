namespace SmartImage.Lib.Engines.Impl.Other
{
	public sealed class KarmaDecayEngine : BaseSearchEngine
	{
		public KarmaDecayEngine() : base("http://karmadecay.com/search/?q=") { }
		
		

		public override SearchEngineOptions Engine => SearchEngineOptions.KarmaDecay;
	}
}