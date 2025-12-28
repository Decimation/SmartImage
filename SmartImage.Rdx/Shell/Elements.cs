using System.Data;
using Kantan.Text;
using Novus.OS;
using Novus.Streams;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Memory;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Images;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Utilities.Integration;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;
using AnsiConsoleExtensions = Spectre.Console.Advanced.AnsiConsoleExtensions;

// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

// ReSharper disable InconsistentNaming

#nullable disable

namespace SmartImage.Rdx.Shell;

internal static class Elements
{

	// Ideally a dictionary would be used here...

#region Colors

	internal static readonly SpcColor Clr_Misc1 = new(0x80, 0xFF, 0x80);

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

#region Text

	internal static readonly Text Txt_Empty = new(String.Empty);

	internal static readonly Text Txt_NA = new(STR_NA);

	internal const string STR_NA = "-";

	internal const double COMPLETE = 100.0d;

#endregion


	static Elements() { }

	internal static Grid AddRowsByChunk(this Grid g, int cnt, params IEnumerable<IRenderable> items)
	{
		var chunks = items.Chunk(cnt);

		foreach (IRenderable[] chunk in chunks) {
			g.AddRow(chunk);
		}

		return g;
	}

	internal static Grid GetInfoGrid()
	{
		var gr = new Grid();
		gr.AddColumns(2);

		var rows = new IRenderable[]
		{
			new Text("User"), new Text($"{Environment.UserName} / {FileSystem.IsRoot}"),
			new Text("Version"), new Text($"{Program.Version}"),
			new Text("Runtime"), new Text($"{Environment.OSVersion} / {Environment.Version}"),
			new Text("Location"), new Text($"{BaseOSIntegration.Executable}")
		};
		gr.AddRowsByChunk(2, rows);


		return gr;
	}


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

		foreach (var (k, v) in dictionary) {
			grd.AddRow(keyFunc(k), valFunc(v));
		}

		return grd;
	}


#region

	public static IRenderable AsRenderableOrText<T>(T val)
	{
		if (val is IRenderable r) {
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

		foreach (var (s, o) in kv) {
			dt.AddRow(new Text(s, Sty_Grid1), new Text(Markup.Escape(FormatObject(o))));
		}

		// Render the layout
		// AnsiConsole.Write(layout);


		return dt;
	}

	internal static CanvasImage GetQueryCanvasImage(UniImage querySource)
	{
		var ci = new CanvasImage(querySource.GetStream())
		{
			// MaxWidth = AnsiConsole.Profile.Width / 4,
			// PixelWidth = 2
		};

		// querySource.Stream.TrySeek();
		return ci;
	}

#region Engine map table

	internal const int ROW_EMT2_THR     = 0;
	internal const int ROW_EMT2_NAME    = 1;
	internal const int ROW_EMT2_RESULTS = 2;
	internal const int ROW_EMT2_STATUS  = 3;
	internal const int ROW_EMT2_TIMEOUT = 4;


	public static SpcTable GetEngineMapTableBase()
	{
		var table = new SpcTable();

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


#region Prompts

	public static readonly TextPrompt<string> Prm_Command = new(Markup.Escape("[Command]"))
	{
		ShowChoices      = true,
		ShowDefaultValue = true,
		AllowEmpty       = false,
		Choices =
		{
			R2.Chc_Open, R2.Chc_Scan, R2.Chc_Preview, R2.Chc_Calc, R2.Chc_Download, R2.Chc_Back, R2.Chc_Exit
		}
	};

	public static readonly TextPrompt<string> Prm_Num2 = new(Markup.Escape("[#.#]"))
	{
		ShowChoices      = false,
		ShowDefaultValue = false,
		AllowEmpty       = false,
	};

	public static readonly SelectionPrompt<SearchResult> Prm_SearchResult = new()
	{
		Mode            = SelectionMode.Independent,
		SearchEnabled   = true,
		PageSize        = 1,
		MoreChoicesText = "...",
		Title           = "Engines",
		WrapAround      = true,
		Converter = static sr =>
		{
			//
			return sr.Engine.Name;
		}
	};

#endregion

	#region Tables

	public static SpcTable CreateMainTable()
	{
		var col = new TableColumn[]
		{
			new(new Text("Engine", Sty_ResultHeader)),
			new(new Text("Results", Sty_ResultHeader)),

		};

		SpcTable tb = CreateResultTable();

		tb.AddColumns(col);

		return tb;
	}

	private static SpcTable CreateResultTable()
	{
		var tb = new SpcTable()
		{
			Caption     = new TableTitle("Results", Sty_ResultHeader),
			Border      = TableBorder.Simple,
			ShowHeaders = true,
		};
		return tb;
	}

	public static SpcTable CreateFullResultTable()
	{
		var col = new TableColumn[]
		{
			new(new Text("Result", Sty_ResultHeader)),
			new(new Text("URL", Sty_ResultHeader)),
			new(new Text("Similarity", Sty_ResultHeader)),
			new(new Text("Artist", Sty_ResultHeader)),
			new(new Text("Resolution", Sty_ResultHeader)),

		};

		var tb = CreateResultTable();

		tb.AddColumns(col);

		return tb;
	}

	#endregion

}