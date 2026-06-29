// Author: Deci | Project: SmartImage.Rdx | Name: SearchCommand.UI.cs
// Date: 2026/06/13 @ 17:06:07

using System.Collections.Concurrent;
using SmartImage.Lib.Engines.Results;
using SmartImage.Rdx.Shell;
using Spectre.Console;

namespace SmartImage.Rdx.Commands.Search;

public sealed partial class SearchCommand
{

	private readonly ConcurrentDictionary<SearchResult, ResultViewState> m_dialogs;

	private Layout m_layout;

	private SpcTable m_mainTable;

	private Layout CreateLayout()
	{
		var queryCi = new CanvasImage(Query.Source.GetSource());

		var ciPanel = new Panel(queryCi)
		{
			Header = new PanelHeader($"{Query.Source.Value}"),
			Expand = true,
		};

		var cfgPanel = new Panel(Renderables.CreateConfigGrid(Config, Query))
		{
			Header = new PanelHeader("Search Options")
		};

		var tblPanel = new Panel(m_mainTable)
		{
			Header = new PanelHeader("Results"),
			Expand = true
		};

		var infoPanel = new Panel(new Text("* [Ctrl+C] Cancel current search"))
		{
			Header = new PanelHeader("Info")
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
			prev = new CanvasImage(Query.Source.GetSource());
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

}