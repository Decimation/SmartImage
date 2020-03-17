using System;

namespace SmartImage
{
	
	[Flags]
	public enum SearchEngines
	{
		None = 0,
		SauceNao = 1 << 0,
		ImgOps = 1 << 1,
		GoogleImages = 1 << 2,
		TinEye = 1 << 3,
		Iqdb = 1 << 4,
		TraceMoe = 1 << 5,
		KarmaDecay = 1 << 6,
	}
}