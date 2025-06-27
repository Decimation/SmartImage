using System.Collections.Concurrent;
using System.Data;
using Kantan.Text;
using Novus.OS;
using Novus.Streams;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Memory;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Images;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities.Integration;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;
using AnsiConsoleExtensions = Spectre.Console.Advanced.AnsiConsoleExtensions;

// ReSharper disable InconsistentNaming

#nullable disable

namespace SmartImage.Rdx.Shell;

internal static class ConsoleFormat
{

	// Ideally a dictionary would be used here...

#region Colors

	public static readonly SpcColor Clr_Misc1 = new(0x80, 0xFF, 0x80);

#endregion

#region Styles

	internal static readonly Style Sty_Name = new(decoration: Decoration.Italic);

	internal static readonly Style Sty_Sim = new(SpcColor.Wheat1, decoration: Decoration.None);

	internal static readonly Style Sty_Url = new(SpcColor.Cyan1, decoration: Decoration.None);

	public static readonly Style Sty_Grid1 = new(foreground: SpcColor.DodgerBlue1, decoration: Decoration.Bold);

	public static readonly Style Sty_Table1 = new(foreground: SpcColor.SpringGreen1, decoration: Decoration.Bold);

	private static readonly Style Sty_Misc1 = new(Clr_Misc1, decoration: Decoration.Underline);

	internal static readonly IReadOnlyDictionary<SearchEngineOptions, SpcColor> EngineColors =
		new Dictionary<SearchEngineOptions, SpcColor>
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

#endregion

#region Text

	internal static readonly Text Txt_Empty = new(String.Empty);

	internal static readonly Text   Txt_NA   = new(STR_NA);
	internal const           string STR_NA   = "-";
	internal const           double COMPLETE = 100.0d;

#endregion


	private static readonly Capabilities ProfileCapabilities;

	static ConsoleFormat()
	{
		ProfileCapabilities = AnsiConsole.Profile.Capabilities;

		InfoMap = new Dictionary<string, object>
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


	}

	internal static readonly Dictionary<string, object> InfoMap;


	internal static Grid MapToGrid<TKey, TValue>(IDictionary<TKey, TValue> dictionary,
	                                             [CBN] Func<TKey, Text> keyFunc = null,
	                                             [CBN] Func<TValue, Text> valFunc = null)
	{
		var grd = new Grid();
		grd.AddColumns(2);

		keyFunc ??= static k =>
		{
			//
			var s = k.ToString();
			ArgumentNullException.ThrowIfNull(s);
			return new Text(s, Sty_Grid1);
		};

		valFunc ??= static v =>
		{
			//
			var s = FormatObject(v);
			ArgumentNullException.ThrowIfNull(s);
			return new Text(s);
		};

		foreach (var (k, v) in dictionary)
		{
			grd.AddRow(keyFunc(k), valFunc(v));
		}

		return grd;
	}


	[MURV]
	public static FigletFont LoadFigletFontFromResource(string name, out MemoryStream fs)
	{
		var o = R2.ResourceManager.GetObject(name);

		if (o == null)
		{
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

		foreach (var property in properties)
		{
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

		foreach (DataColumn row in dt.Columns)
		{
			t.AddColumn(new TableColumn(row.ColumnName));
		}

		Func<object, IRenderable> selector = AsRenderableOrText;

		foreach (DataRow row in dt.Rows)
		{
			var obj = row.ItemArray
				.Select(selector);

			t.AddRow(obj);
		}

		return t;
	}

	internal static SpcColor GetEngineColor(SearchEngineOptions opt)
	{
		if (!EngineColors.TryGetValue(opt, out var color))
		{
			color = SpcColor.White;
		}

		return color;
	}

#region

	public static IRenderable AsRenderableOrText<T>(T val)
	{
		if (val is IRenderable r)
		{
			return r;
		}

		var s    = val?.ToString();
		var text = s == null ? Txt_Empty : new Text(s);
		return text;
	}

	private static string FormatObject(object o)
	{
		return o switch

		{
			null   => STR_NA,
			bool b => ToCheck(b),
			_      => o.ToString(),
		};
	}

	public static string ToCheck(bool b)
	{
		return (b ? Strings.Constants.RAD_SIGN : Strings.Constants.MUL_SIGN).ToString();
	}

#endregion

	internal static Grid CreateConfigGrid(SearchConfig cfg, SearchQuery query)
	{
		var dt = new Grid();
		dt.AddColumns(2);

		var kv = new Dictionary<string, object>
		{
			[R1.S_SearchEngines]   = cfg.SearchEngines,
			[R1.S_PriorityEngines] = cfg.PriorityEngines,
			[R1.S_AutoSearch]      = cfg.AutoSearch,
			[R1.S_ReadCookies]     = cfg.ReadCookies,

			["Input"]  = query,
			["Upload"] = query.Upload,

			["FlareSolverr"] = cfg.FlareSolverr
		};

		foreach (var (s, o) in kv)
		{
			dt.AddRow(new Text(s, Sty_Grid1),
			          new Text(Markup.Escape(FormatObject(o))));
		}

		// Render the layout
		// AnsiConsole.Write(layout);


		return dt;
	}

	internal static CanvasImage GetQueryCanvasImage(UniImage querySource)
	{
		using var ms = querySource.Image.ToStream(); //todo

		// var       sp = new Span<byte>();
		// querySource.Image.CopyPixelDataTo(sp);


		var ci = new CanvasImage(ms)
		{
			MaxWidth = AnsiConsole.Profile.Width / 4,

			// PixelWidth = 2
		};

		// querySource.Stream.TrySeek();
		return ci;
	}

#region Engine map table

	public static (ConcurrentDictionary<BaseSearchEngine, int>, STable) GetEngineMapTable(BaseSearchEngine[] engines)
	{
		var engineMap = new ConcurrentDictionary<BaseSearchEngine, int>();
		var table     = GetEngineMapTableBase();

		int i = 0;

		foreach (BaseSearchEngine engine in engines)
		{
			table.AddRow(Txt_NA, new Text(engine.Name, GetEngineColor(engine.EngineOption)), Txt_NA, Txt_NA, new Text(engine.Timeout.ToString()));

			engineMap.TryAdd(engine, i++);
		}

		return (engineMap, table);
	}

	public const int ROW_EMT2_THR     = 0;
	public const int ROW_EMT2_NAME    = 1;
	public const int ROW_EMT2_RESULTS = 2;
	public const int ROW_EMT2_STATUS  = 3;
	public const int ROW_EMT2_TIMEOUT = 4;


	public static STable GetEngineMapTableBase()
	{
		var table = new STable();

		var columns = GetColumns("Thread", nameof(BaseSearchEngine.Name), nameof(SearchResult.Results),
		                         nameof(SearchResult.Status), nameof(BaseSearchEngine.Timeout));


		table.AddColumns(columns.ToArray());
		return table;
	}

	private static TableColumn GetColumn(string name)
	{
		return new TableColumn(new Text(name, Sty_Grid1)) { };
	}

	public static IEnumerable<TableColumn> GetColumns(params string[] names)
	{
		return names.Select(GetColumn);
	}

#endregion

	public static async Task WriteFigletGradientAsync(FigletFont ff, string text, SpcColor a, SpcColor b, TimeSpan delta)
	{
		var col = new Queue<SpcColor>(a.Interpolate(b, (byte) text.Length));
		(int left, int top) = Console.GetCursorPosition();

		//todo
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];

			var color = col.Dequeue();

			var ft = new FigletText(ff, $"{c}")
			{
				Color         = color,
			};
			
			var fts=ft.GetSegments(AnsiConsole.Console);
			// AnsiConsole.Cursor.SetPosition(left +(ff.Height *i),top +(ff.MaxWidth *i));
			AnsiConsole.Write(ft);
			// AnsiConsole.Console.Clear(false);

			AnsiConsole.Console.Cursor.Move(CursorDirection.Up, ff.Height+1);
			AnsiConsole.Console.Cursor.Move(CursorDirection.Right, ff.MaxWidth * (i + 1));

			await Task.Delay(delta);
		}

	}

}