using System.Data;
using System.Diagnostics;
using JetBrains.Annotations;
using Novus.OS;
using Novus.Streams;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities.Integration;
using SmartImage.Rdx.Commands;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

// ReSharper disable InconsistentNaming
#nullable disable
namespace SmartImage.Rdx.Shell;

internal static class ConsoleFormat
{

	// Ideally a dictionary would be used here...

	#region Colors

	public static readonly Color Clr_Misc1 = new(0x80, 0xFF, 0x80);

	#endregion

	#region Styles

	internal static readonly Style Sty_Name = new(decoration: Decoration.Italic);

	internal static readonly Style Sty_Sim = new(Color.Wheat1, decoration: Decoration.None);

	internal static readonly Style Sty_Url = new(Color.Cyan1, decoration: Decoration.None);

	public static readonly Style Sty_Grid1 = new(foreground: Color.DodgerBlue1, decoration: Decoration.Bold);

	private static readonly Style Sty_Misc1 = new(Clr_Misc1, decoration: Decoration.Underline);

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
			{ SearchEngineOptions.Fluffle, Color.LightYellow3 },
			{ SearchEngineOptions.TinEye, Color.SkyBlue1 },

		}.AsReadOnly();

	#endregion

	#region Text

	internal static readonly Text Txt_Empty = new(string.Empty);

	internal static readonly Text Txt_NA = new(STR_NA);

	#endregion


	static ConsoleFormat() { }

	private static readonly Capabilities ProfileCapabilities = AnsiConsole.Profile.Capabilities;

	internal static readonly Dictionary<string, object> InfoMap = new()
	{
		["OS"]               = $"{Environment.OSVersion}",
		["User"]             = $"{Environment.UserName} / {FileSystem.IsRoot}",
		["Runtime"]          = Environment.Version,
		["Terminal ANSI"]    = ProfileCapabilities.Ansi,
		["Terminal colors"]  = ProfileCapabilities.ColorSystem,
		["Terminal links"]   = ProfileCapabilities.Links,
		["Terminal Unicode"] = ProfileCapabilities.Unicode,
		["Version"]          = $"{Program.Version}",
		["Location"]         = BaseOSIntegration.Executable
	};


	internal static Grid MapToGrid<TKey, TValue>(IDictionary<TKey, TValue> dictionary,
	                                             [CBN] Func<TKey, Text> keyFunc = null,
	                                             [CBN] Func<TValue, Text> valFunc = null)
	{
		var grd = new Grid();
		grd.AddColumns(2);

		keyFunc ??= static (k) =>
		{
			//
			var s = k.ToString();
			ArgumentNullException.ThrowIfNull(s);
			return new Text(s, Sty_Grid1);
		};

		valFunc ??= static (v) =>
		{
			//
			var s = v.ToString();
			ArgumentNullException.ThrowIfNull(s);
			return new Text(s);
		};

		foreach (var (k, v) in dictionary) {
			grd.AddRow(keyFunc(k), valFunc(v));
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

	internal static void Dump(CommandSettings settings)
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

	public static STable DTableToSTable(DTable dt)
	{
		var t = new STable();

		foreach (DataColumn row in dt.Columns) {
			t.AddColumn(new TableColumn(row.ColumnName));
		}

		Func<object, IRenderable> selector = AsRenderableOrText;

		foreach (DataRow row in dt.Rows) {
			var obj = row.ItemArray
				.Select(selector);

			t.AddRow(obj);
		}

		return t;
	}

	public static IRenderable AsRenderableOrText<T>(T val)
	{
		if (val is IRenderable r) {
			return r;
		}

		var s    = val?.ToString();
		var text = s == null ? Txt_Empty : new Text(s);
		return text;
	}

	internal static Color GetEngineColor(SearchEngineOptions opt)
	{
		if (!EngineColors.TryGetValue(opt, out var color)) {
			color = Color.White;
		}

		return color;
	}

	internal const string STR_NA   = "-";
	internal const double COMPLETE = 100.0d;

	internal static Grid CreateConfigGrid(SearchConfig cfg, SearchQuery query)
	{
		var dt = new Grid();
		dt.AddColumns(2);

		var kv = new Dictionary<string, object>()
		{
			[R1.S_SearchEngines]   = cfg.SearchEngines,
			[R1.S_PriorityEngines] = cfg.PriorityEngines,
			[R1.S_AutoSearch]      = cfg.AutoSearch,
			[R1.S_ReadCookies]     = cfg.ReadCookies,

			["Input"]  = query,
			["Upload"] = query.Upload
		};

		foreach (var o in kv) {
			dt.AddRow(GetRowsForKeyValue(o));
		}


		// Render the layout
		// AnsiConsole.Write(layout);


		return dt;
	}

	public static IRenderable[] GetRowsForKeyValue(KeyValuePair<string, object> o)
	{
		return
		[
			new Text(o.Key, ConsoleFormat.Sty_Grid1),
			new Text(Markup.Escape(o.Value?.ToString()))
			{
				Overflow = Overflow.Crop,
			}
		];
	}

	internal static CanvasImage GetQueryCanvasImage(UniImage querySource)
	{
		var ci = new CanvasImage(querySource.Stream)
		{
			MaxWidth = AnsiConsole.Profile.Width / 6,

			// PixelWidth = 2
		};
		querySource.Stream.TrySeek();
		return ci;
	}

}