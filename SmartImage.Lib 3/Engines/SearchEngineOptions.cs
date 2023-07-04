// ReSharper disable UnusedMember.Global

using SmartImage.Lib.Engines.Impl.Search;
using SmartImage.Lib.Engines.Impl.Search.Other;

namespace SmartImage.Lib.Engines;

/// <summary>
///     Search engine options
/// </summary>
[Flags]
public enum SearchEngineOptions
{
	/// <summary>
	///     No engines
	/// </summary>
	None = 0,

	/// <summary>
	///     Automatic (use best result)
	/// </summary>
	Auto = 1,

	/// <summary>
	///     <see cref="SauceNaoEngine" />
	/// </summary>
	SauceNao = 1 << 1,

	/// <summary>
	///     <see cref="ImgOpsEngine" />
	/// </summary>
	ImgOps = 1 << 2,

	/// <summary>
	///     <see cref="GoogleImagesEngine" />
	/// </summary>
	GoogleImages = 1 << 3,

	/// <summary>
	///     <see cref="TinEyeEngine" />
	/// </summary>
	TinEye = 1 << 4,

	/// <summary>
	///     <see cref="IqdbEngine" />
	/// </summary>
	Iqdb = 1 << 5,

	/// <summary>
	///     <see cref="TraceMoeEngine" />
	/// </summary>
	TraceMoe = 1 << 6,

	/// <summary>
	///     <see cref="KarmaDecayEngine" />
	/// </summary>
	KarmaDecay = 1 << 7,

	/// <summary>
	///     <see cref="YandexEngine" />
	/// </summary>
	Yandex = 1 << 8,

	/// <summary>
	///     <see cref="BingEngine" />
	/// </summary>
	Bing = 1 << 9,

	/// <summary>
	///     <see cref="Ascii2DEngine" />
	/// </summary>
	Ascii2D = 1 << 10,

	/// <summary>
	/// <see cref="RepostSleuthEngine"/>
	/// </summary>
	RepostSleuth = 1 << 11,

	/// <summary>
	/// <see cref="EHentaiEngine"/>
	/// </summary>
	EHentai = 1 << 12,

	/// <summary>
	/// <see cref="ArchiveMoeEngine"/>
	/// </summary>
	ArchiveMoe = 1 << 13,

	/// <summary>
	///     All engines
	/// </summary>
	All = SauceNao | ImgOps | GoogleImages | TinEye | Iqdb | TraceMoe | KarmaDecay | Yandex | Bing |
	      Ascii2D | RepostSleuth | EHentai | ArchiveMoe,

	Artwork = SauceNao | Iqdb | Ascii2D | EHentai,
}