using System.Data;
using System.Data.SqlTypes;
using Kantan.Text;
using Novus.OS;
using Novus.Streams;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Memory;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Images;
using SmartImage.Lib.Utilities.Integration;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;
using Kantan.Console;
using SmartImage.Lib.Engines.Search.Base;

// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
// ReSharper disable InconsistentNaming

#nullable disable

namespace SmartImage.Rdx.Shell;

internal static class ElementStyles
{

#region Colors

	internal static readonly SpcColor Clr_Misc1 = new(0x80, 0xFF, 0x80);

	internal static readonly IReadOnlyDictionary<SearchEngineOptions, SpcColor> EngineColors = new Dictionary<SearchEngineOptions, SpcColor>
	{
		{ SearchEngineOptions.SauceNao, SpcColor.Green },
		{ SearchEngineOptions.EHentai, SpcColor.Purple },
		{ SearchEngineOptions.Iqdb, SpcColor.LightGreen },
		{ SearchEngineOptions.Ascii2D, SpcColor.Cyan1 },
		{ SearchEngineOptions.TraceMoe, SpcColor.DodgerBlue1 },
		{ SearchEngineOptions.RepostSleuth, SpcColor.RosyBrown },
		{ SearchEngineOptions.ArchiveMoe, SpcColor.Wheat1 },
		{ SearchEngineOptions.Yandex, SpcColor.Orange1 },
		{ SearchEngineOptions.Iqdb3D, SpcColor.SeaGreen1 },
		{ SearchEngineOptions.Fluffle, SpcColor.LightYellow3 },
		{ SearchEngineOptions.TinEye, SpcColor.SkyBlue1 },

	}.AsReadOnly();

	internal static SpcColor GetColor(this SearchEngineOptions opt)
	{
		if (!EngineColors.TryGetValue(opt, out var color)) {
			color = SpcColor.White;
		}

		return color;
	}

#endregion

#region Styles

	internal static readonly Style Sty_Name = new(decoration: Decoration.Italic);

	internal static readonly Style Sty_Sim = new(SpcColor.Wheat1, decoration: Decoration.None);

	internal static readonly Style Sty_Url = new(SpcColor.Cyan1, decoration: Decoration.None);

	internal static readonly Style Sty_Grid1 = new(foreground: SpcColor.DodgerBlue1, decoration: Decoration.Bold);

	internal static readonly Style Sty_Table1 = new(foreground: SpcColor.SpringGreen1, decoration: Decoration.Bold);

	internal static readonly Style Sty_Misc1 = new(Clr_Misc1, decoration: Decoration.Underline);

	internal static readonly Style Sty_RootResults = new(foreground: SpcColor.Aqua, decoration: Decoration.Underline);

	internal static readonly Style Sty_ResultHeader = new(decoration: Decoration.Bold, background: SpcColor.Blue, foreground: SpcColor.White);

#endregion

	internal const double COMPLETE = 100.0d;

}