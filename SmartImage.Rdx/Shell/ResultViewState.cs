// Author: Deci | Project: SmartImage.Rdx | Name: ResultViewState.cs
// Date: 2026/03/21 @ 14:03:16

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Engines.Results;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace SmartImage.Rdx.Shell;

public class ResultViewState
{

	public SpcTable Table { get; }

	public ConcurrentDictionary<IResultItem, SelectionPrompt<string>> Commands { get; }

	private ResultViewState(SpcTable table)
	{
		Table    = table;
		Commands = new ConcurrentDictionary<IResultItem, SelectionPrompt<string>>();
	}

	public static ResultViewState Create(SearchResult result)
	{
		var fullRows = result.GetFullResultRows();

		var table = Renderables.CreateResultTable();

		foreach (IRenderable[] row in fullRows) {
			table.AddRow(row);
		}


		return new ResultViewState(table);
	}

}