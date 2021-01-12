using System;

namespace SmartImage.Engines
{
	[Flags]
	public enum SearchEngineOptions
	{
		/*
		 * Special values
		 */
		
		None = 0,
		Auto = 1,
		
		/*
		 * Engines
		 */
		
		SauceNao = 1 << 1,
		ImgOps = 1 << 2,
		GoogleImages = 1 << 3,
		TinEye = 1 << 4,
		Iqdb = 1 << 5,
		TraceMoe = 1 << 6,
		KarmaDecay = 1 << 7,
		Yandex = 1 << 8,
		Bing = 1 << 9,

		/*
		 * All
		 */
		
		All = SauceNao | ImgOps | GoogleImages | TinEye | Iqdb | TraceMoe | KarmaDecay | Yandex | Bing
	}
}