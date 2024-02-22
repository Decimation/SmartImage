// Deci SmartImage.Rdx CliFormat.Elements.cs
// $File.CreatedYear-$File.CreatedMonth-26 @ 1:25

using SmartImage.Lib.Engines;
using Spectre.Console;

namespace SmartImage.Rdx.Cli;

internal static partial class CliFormat
{

	// Ideally a dictionary would be used here...

	internal static readonly Style Sty_Name = new(decoration: Decoration.Italic);

	internal static readonly Style Sty_Sim = new(Color.Wheat1, decoration: Decoration.None);

	internal static readonly Style Sty_Url = new(Color.Cyan1, decoration: Decoration.None);

	public static readonly Color Clr_Misc1 = new(0x80, 0xFF, 0x80);

	internal static readonly Text Txt_Empty = new(string.Empty);

	internal const string STR_DEFAULT = "-";

	static CliFormat() { }

	internal static readonly IReadOnlyDictionary<SearchEngineOptions, Color> EngineColors =
		new Dictionary<SearchEngineOptions, Color>
		{
			{ SearchEngineOptions.SauceNao, Color.Green },
			{ SearchEngineOptions.EHentai, Color.Purple },
			{ SearchEngineOptions.Iqdb, Color.LightGreen },
			{ SearchEngineOptions.Ascii2D, Color.Cyan1 },
			{ SearchEngineOptions.TraceMoe, Color.DodgerBlue1 },
			{ SearchEngineOptions.RepostSleuth, Color.RosyBrown },
			{ SearchEngineOptions.ArchiveMoe, Color.Wheat1 },
			{ SearchEngineOptions.Yandex, Color.Orange1 },
			{ SearchEngineOptions.Iqdb3D, Color.SeaGreen1 },

		};

	internal static readonly Capabilities ProfileCapabilities = AConsole.Profile.Capabilities;

	internal static readonly bool IsLinux   = OperatingSystem.IsLinux();
	internal static readonly bool IsWindows = OperatingSystem.IsWindows();
	internal static readonly bool IsMacOs = OperatingSystem.IsMacOS();

	public static Grid CreateInfoGrid()
	{
		var grd = new Grid();
		grd.AddColumns(2);

		grd.AddRow("OS", $"{GetOS()} / {Environment.OSVersion}");
		grd.AddRow("Runtime", $"{Environment.Version}");

		grd.AddRow("Terminal ANSI", $"{ProfileCapabilities.Ansi}");
		grd.AddRow("Terminal colors", $"{ProfileCapabilities.ColorSystem}");
		grd.AddRow("Terminal links", $"{ProfileCapabilities.Links}");
		grd.AddRow("Terminal Unicode", $"{ProfileCapabilities.Unicode}");

		return grd;
	}

}