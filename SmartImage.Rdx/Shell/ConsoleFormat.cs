using System.Data;
using System.Reflection;
using Kantan.Utilities;
using Novus.OS;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

// ReSharper disable InconsistentNaming

namespace SmartImage.Rdx.Shell;

internal static class ConsoleFormat
{

	// Ideally a dictionary would be used here...

	internal static readonly Style Sty_Name = new(decoration: Decoration.Italic);

	internal static readonly Style Sty_Sim = new(Color.Wheat1, decoration: Decoration.None);

	internal static readonly Style Sty_Url = new(Color.Cyan1, decoration: Decoration.None);

	public static readonly Style Sty_Grid1 = new(foreground: Color.DodgerBlue1, decoration: Decoration.Bold);

	public static readonly Color Clr_Misc1 = new(0x80, 0xFF, 0x80);

	internal static readonly Text Txt_Empty = new(string.Empty);

	internal const string STR_DEFAULT = "-";

	static ConsoleFormat() { }

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

	internal static Grid CreateInfoGrid()
	{
		var grd = new Grid();
		grd.AddColumns(2);

		var dict = new Dictionary<string, object>()
		{
			["OS"]               = $"{AppUtil.GetOSName()} / {Environment.OSVersion}",
			["User"]             = $"{Environment.UserName} / {FileSystem.IsRoot}",
			["Runtime"]          = Environment.Version,
			["Terminal ANSI"]    = ProfileCapabilities.Ansi,
			["Terminal colors"]  = ProfileCapabilities.ColorSystem,
			["Terminal links"]   = ProfileCapabilities.Links,
			["Terminal Unicode"] = ProfileCapabilities.Unicode,
			["Version"]          = $"{SearchCommand.Version}",
			["Location"]         = AppUtil.ExeLocation
		};

		foreach ((string? key, var value) in dict) {
			grd.AddRow(new Text(key, Sty_Grid1), new Text(value.ToString()));
		}

		return grd;
	}

	[MURV]
	public static FigletFont LoadFigletFontFromResource(string name, out MemoryStream fs)
	{
		var o = R2.ResourceManager.GetObject(name);

		if (o == null) {
			throw new InvalidOperationException(nameof(name));
		}

		fs = new MemoryStream((byte[]) o);
		var ff = FigletFont.Load(fs);

		return ff;
	}

	public static void Dump(CommandSettings settings)
	{
		var table = new STable().RoundedBorder();
		table.AddColumn("[grey]Name[/]");
		table.AddColumn("[grey]Value[/]");

		var properties = settings.GetType().GetProperties();

		foreach (var property in properties) {
			var value = property.GetValue(settings)
				?.ToString()
				?.Replace("[", "[[");

			table.AddRow(
				property.Name,
				value ?? "[grey]null[/]");
		}

		AnsiConsole.Write(table);
	}

	public static STable GetTableForFormat(OutputFields format)
	{

		var fmt   = format.GetSetFlags(true, true);
		var table = new STable();
		var col   = new TableColumn[fmt.Count];

		for (int i = 0; i < col.Length; i++) {
			col[i] = new TableColumn(new Text($"{fmt[i]}",
			                                  new Style(decoration: Decoration.Bold | Decoration.Underline)));
		}

		table.AddColumns(col);

		return table;
	}

	public static STable DTableToSTable(DTable dt)
	{
		var t = new STable();

		foreach (DataColumn row in dt.Columns) {
			t.AddColumn(new TableColumn(row.ColumnName));
		}

		foreach (DataRow row in dt.Rows) {
			var obj = row.ItemArray
				.Select(x =>
				{
					if (x is IRenderable r) {
						return r;
					}

					if (x == null) {
						return Txt_Empty;
					}

					return new Text(x.ToString());
				});

			t.AddRow(obj);
		}

		return t;
	}

}