using System.Reflection;
using SmartImage.Lib.Engines;
using Spectre.Console;

namespace SmartImage.Rdx.Shell;

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

	internal static readonly IReadOnlyDictionary<SearchEngineOptions, Style> EngineStyles =
		new Dictionary<SearchEngineOptions, Style>
		{
			{ SearchEngineOptions.SauceNao, new Style(Color.Green) },
			{ SearchEngineOptions.EHentai, new Style(Color.Purple) },
			{ SearchEngineOptions.Iqdb, new Style(Color.LightGreen) },
			{ SearchEngineOptions.Ascii2D, new Style(Color.Cyan1) },
			{ SearchEngineOptions.TraceMoe, new Style(Color.DodgerBlue1) },
			{ SearchEngineOptions.RepostSleuth, new Style(Color.RosyBrown) },
			{ SearchEngineOptions.ArchiveMoe, new Style(Color.Wheat1) },
			{ SearchEngineOptions.Yandex, new Style(Color.Orange1) },
			{ SearchEngineOptions.Iqdb3D, new Style(Color.SeaGreen1) },
			{ SearchEngineOptions.Fluffle, new Style(Color.LightYellow3) },

		};
	internal static readonly Capabilities ProfileCapabilities = AConsole.Profile.Capabilities;

	internal static readonly bool     IsLinux    = OperatingSystem.IsLinux();
	internal static readonly bool     IsWindows  = OperatingSystem.IsWindows();
	internal static readonly bool     IsMacOs    = OperatingSystem.IsMacOS();
	private static readonly  Assembly s_assembly = Assembly.GetExecutingAssembly();
	private static readonly  string   s_version  = s_assembly.GetName().Version.ToString();

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
			["Terminal Unicode"] = ProfileCapabilities.Unicode,
			["Version"]          = $"{s_version}"
		};

		foreach ((string? key, var value) in dict) {
			grd.AddRow(new Text(key, Sty_Grid1), new Text(value.ToString()));
		}

		return grd;
	}

}