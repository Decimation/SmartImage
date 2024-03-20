
using SmartImage.Lib.Engines;
using Spectre.Console;

namespace SmartImage.Rdx.Cli;

internal static partial class CliFormat
{

	// Ideally a dictionary would be used here...

	internal static readonly Style Sty_Name = new(decoration: Decoration.Italic);

	internal static readonly Style Sty_Sim = new(Color.Wheat1, decoration: Decoration.None);

	internal static readonly Style Sty_Url = new(Color.Cyan1, decoration: Decoration.None);

	public static readonly Style Sty_Grid1 = new Style(foreground: Color.DodgerBlue1, decoration: Decoration.Bold);

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
	internal static readonly bool IsMacOs   = OperatingSystem.IsMacOS();

	public static Grid CreateInfoGrid()
	{
		var grd = new Grid();
		grd.AddColumns(2);

		var dict = new Dictionary<string, object>()
		{
			["OS"]               = $"{GetOS()} / {Environment.OSVersion}",
			["Runtime"]          = Environment.Version,
			["Terminal ANSI"]    = ProfileCapabilities.Ansi,
			["Terminal colors"]  = ProfileCapabilities.ColorSystem,
			["Terminal links"]   = ProfileCapabilities.Links,
			["Terminal Unicode"] = ProfileCapabilities.Unicode
		};

		foreach ((string? key, var value) in dict) {
			grd.AddRow(new Text(key, Sty_Grid1), new Text(value.ToString()));
		}

		return grd;
	}

}