using System;

namespace SmartImage.Engines
{
	/// <summary>
	/// Search engine options
	/// </summary>
	[Flags]
	public enum SearchEngineOptions
	{
		/// <summary>
		/// No engines
		/// </summary>
		None = 0,

		/// <summary>
		/// Automatic (use best result)
		/// </summary>
		Auto = 1,


		/// <summary>
		///     <list type="bullet">
		///         <item>
		///             <description>
		///                 <see cref="Engines.SauceNao" />
		///             </description>
		///         </item>
		///         <item>
		///             <description>
		///                 <see cref="Engines.SauceNao.SauceNaoEngine" />
		///             </description>
		///         </item>
		///     </list>
		/// </summary>
		SauceNao = 1 << 1,

		/// <summary>
		///     <see cref="Engines.Other.ImgOpsEngine" />
		/// </summary>
		ImgOps = 1 << 2,

		/// <summary>
		///     <see cref="Engines.Other.GoogleImagesEngine" />
		/// </summary>
		GoogleImages = 1 << 3,


		/// <summary>
		///     <see cref="Engines.Other.TinEyeEngine" />
		/// </summary>
		TinEye = 1 << 4,


		/// <summary>
		///     <see cref="Engines.Other.IqdbEngine" />
		/// </summary>
		Iqdb = 1 << 5,

		/// <summary>
		///     <list type="bullet">
		///         <item>
		///             <description>
		///                 <see cref="Engines.TraceMoe" />
		///             </description>
		///         </item>
		///         <item>
		///             <description>
		///                 <see cref="Engines.TraceMoe.TraceMoeEngine" />
		///             </description>
		///         </item>
		///     </list>
		/// </summary>
		TraceMoe = 1 << 6,

		/// <summary>
		///     <see cref="Engines.Other.KarmaDecayEngine" />
		/// </summary>
		KarmaDecay = 1 << 7,


		/// <summary>
		///     <see cref="Engines.Other.YandexEngine" />
		/// </summary>
		Yandex = 1 << 8,


		/// <summary>
		///     <see cref="Engines.Other.BingEngine" />
		/// </summary>
		Bing = 1 << 9,


		Tidder = 1 << 10,

		/// <summary>
		/// All engines
		/// </summary>
		All = SauceNao | ImgOps | GoogleImages | TinEye | Iqdb | TraceMoe | KarmaDecay | Yandex | Bing | Tidder
	}
}