// Author: Deci | Project: SmartImage.Rdx | Name: SearchCommand.UI.cs
// Date: 2026/06/13 @ 17:06:07

using Kantan.Console;
using Novus.OS;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Results;
using SmartImage.Rdx.Shell;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Collections.Concurrent;

namespace SmartImage.Rdx.Commands.Search;

public sealed partial class SearchCommand
{

	private readonly ConcurrentDictionary<SearchResult, ResultViewState> m_dialogs;

	private Layout m_layout;

	private SpcTable m_mainTable;

	private Layout CreateLayout()
	{

		var ciPanel = new Panel(m_queryCanvasImg)
		{
			Header = new PanelHeader($"{Query.AllocImage.Value}"),
			Expand = true,
		};

		var cfgPanel = new Panel(CreateConfigGrid(Config, Query))
		{
			Header  = new PanelHeader("Search Options"),
			Padding = null
		};

		var tblPanel = new Panel(m_mainTable)
		{
			Header = new PanelHeader("Results"),
			Expand = true
		};

		var infoPanel = new Panel(new Text("* [Ctrl+C] Cancel current search"))
		{
			Header = new PanelHeader("Info"),
			Expand = false
		};

		return new Layout("Root")
			.SplitColumns(
				new Layout("L").SplitRows(
					new("LC", cfgPanel),
					new("LT", tblPanel),
					new Layout("LI", infoPanel)
				),
				new Layout("R", ciPanel));
	}

	private Layout GetExpandedLayout(IResultItem sri)
	{
		CanvasImage prev;

		if (sri is ScannedResultItem scnItem) {
			prev = GetPreviewCanvasImage(scnItem);
		}
		else {
			prev = m_queryCanvasImg;
		}

		var ciPanel = new Panel(prev)
		{
			Header = new PanelHeader($"{sri}"),
			Expand = true,
		};

		var extGrid = sri.CreateExtendedGrid();

		var extPanel = new Panel(extGrid) { Header = new PanelHeader("Result Data") };

		var exLayout = new Layout("Root")
			.SplitColumns(
				new Layout("L", extPanel),
				new Layout("R", ciPanel));

		return exLayout;
	}

	internal static SpcTable CreateEmptyResultTable()
	{
		var tb = new SpcTable()
		{
			// Caption     = new TableTitle("Results", ElementStyles.Sty_ResultHeader),
			Border      = TableBorder.Simple,
			ShowHeaders = true,
		};
		return tb;
	}

	internal static SpcTable CreateOverviewTable()
	{
		SpcTable tb = CreateEmptyResultTable();

		tb = tb.AddColumns(s_overviewTableColumns);

		return tb;
	}

	internal static SpcTable CreateResultTable()
	{
		var tb = CreateEmptyResultTable();

		tb = tb.AddColumns(s_resultTableColumns);

		return tb;
	}

	internal static Grid CreateEnvironmentGrid()
	{
		var gr = new Grid();
		gr.AddColumns(2);
		gr.AddRowsByChunk(2, s_envGridRows);

		return gr;
	}

	internal static Grid CreateConfigGrid(SearchConfig cfg, SearchQuery query)
	{
		var dt = new Grid();
		dt.AddColumns(2);

		dt.AddRow(new Text("Query", ElementStyles.Sty_Grid1), Renderables.MarkupLink(query.AllocImage.Value));
		dt.AddRow(new Text("Query Format", ElementStyles.Sty_Grid1), new Text($"({query.AllocImage.Type}) {query.AllocImage.ImageFormat.Name}"));
		dt.AddRow(new Text("Upload", ElementStyles.Sty_Grid1), Renderables.MarkupLink(query.Upload.Url, query.Upload.ToString()));


		var kv = new Dictionary<object, object>
		{
			[R1.S_SearchEngines]   = cfg.SearchEngines,
			[R1.S_PriorityEngines] = cfg.PriorityEngines,
			[R1.S_UploadEngine]    = cfg.UploadEngine,
			[R1.S_AutoSearch]      = cfg.AutoSearch,
			[R1.S_ReadCookies]     = cfg.ReadCookies,
			["FlareSolverr"]       = cfg.FlareSolverr,
		};

		foreach (var (s, o) in kv) {
			IRenderable kR;

			if (s is Text txt) {
				kR = txt;
			}
			else {

				kR = String.IsNullOrWhiteSpace(s?.ToString()) ? ElementUtility.Txt_NA : new Text(s.ToString() ?? String.Empty);
			}

			dt.AddRow(kR, RenderableUtility.AsRenderable(o));
		}

		return dt;
	}

	private static readonly TableColumn[] s_resultTableColumns =
	[
		new(new Text("Result", ElementStyles.Sty_ResultHeader)),
		new(new Text("URL", ElementStyles.Sty_ResultHeader)),
		new(new Text("Similarity", ElementStyles.Sty_ResultHeader)),
		new(new Text("Artist", ElementStyles.Sty_ResultHeader)),
		new(new Text("Resolution", ElementStyles.Sty_ResultHeader))

	];

	private static readonly TableColumn[] s_overviewTableColumns =
	[
		new(new Text("Engine", ElementStyles.Sty_ResultHeader)),
		new(new Text("Results", ElementStyles.Sty_ResultHeader))
	];

	private static readonly IRenderable[] s_envGridRows =
	[
		new Text("User", ElementStyles.Sty_Grid1), new Text($"{Environment.UserName} / {FileSystem.IsRoot}"),
		new Text("Version", ElementStyles.Sty_Grid1), new Text($"{Program.Version}"),
		new Text("Runtime", ElementStyles.Sty_Grid1), new Text($"{Environment.OSVersion} / {Environment.Version}"),
		new Text("Location", ElementStyles.Sty_Grid1), new TextPath(Environment.ProcessPath ?? String.Empty)
	];

}