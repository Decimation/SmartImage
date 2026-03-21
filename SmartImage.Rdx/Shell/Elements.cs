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
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Utilities.Integration;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;
using AnsiConsoleExtensions = Spectre.Console.Advanced.AnsiConsoleExtensions;
using Kantan.Console;
using SmartImage.Lib.Engines.Search.Base;

// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
// ReSharper disable InconsistentNaming

#nullable disable

namespace SmartImage.Rdx.Shell;

internal static class Elements
{

#region Colors

	internal static readonly SpcColor Clr_Misc1 = new(0x80, 0xFF, 0x80);

	public static readonly IReadOnlyDictionary<SearchEngineOptions, SpcColor> EngineColors = new Dictionary<SearchEngineOptions, SpcColor>
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


#region Prompts

	public static readonly TextPrompt<string> Prm_Command = new(Markup.Escape("[Command]"))
	{
		ShowChoices      = true,
		ShowDefaultValue = true,
		AllowEmpty       = false,
		Choices =
		{
			R2.Chc_Open, R2.Chc_Scan, R2.Chc_Preview, R2.Chc_Calc, R2.Chc_Download, R2.Chc_Expand, R2.Chc_Back, R2.Chc_Exit
		}
	};

	public static readonly TextPrompt<string> Prm_Selection = new(Markup.Escape("[#.#]"))
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
		Title           = null,
		SearchPlaceholderText = null,
		WrapAround      = true,
		Converter = static sr =>
		{
			//
			return sr.Engine.Name;
		}
	};

#endregion

#if SERVER
	
#region Engine map table

	//  TODO: FOR SERVER ONLY, DEPRECATE

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
#endif

}