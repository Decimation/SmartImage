using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartImage.Lib;
using Spectre.Console;

namespace SmartImage.Shell;

public static class Gui
{
	private static readonly Style PromptStyle = Style.Parse("underline");

	public static readonly TextPrompt<string> Prompt = new("Input:")
	{
		AllowEmpty = false,
		Validator = static s =>
		{
			try {
				var task  = SearchQuery.TryCreateAsync(s);
				var query = task.Result;
				Program.Query = query;
				return ValidationResult.Success();
			}
			catch (Exception e) {
				return ValidationResult.Error($"Error: {e.Message}");
			}
		},
		PromptStyle = PromptStyle,
	};

	public static readonly MultiSelectionPrompt<SearchEngineOptions> Prompt2 = new()
	{
		Converter = option =>
		{
			return option.ToString();
		},
		Title = "Engines:",
		PageSize = 15,
	};

	public static readonly TextPrompt<bool> Prompt3 = new("Stay on top")
	{
		AllowEmpty       = true,
		ShowDefaultValue = true,
		PromptStyle      = PromptStyle,
	};

	public static readonly Table ResultsTable = new()
	{

		Border      = TableBorder.Heavy,
		BorderStyle = Style.Plain
	};

	static Gui()
	{
		Prompt2 = Prompt2.AddChoices(Enum.GetValues<SearchEngineOptions>());
		Prompt3 = Prompt3.DefaultValue(SearchConfig.ON_TOP_DEFAULT);
	}

	public static async Task LiveCallback(LiveDisplayContext ctx)
	{
		Gui.ResultsTable.AddColumns("[bold]Engine[/]", "[bold]Info[/]", nameof(SearchResult.Results));

		while (!Program.Status) {
			ctx.Refresh();
			await Task.Delay(TimeSpan.FromMilliseconds(100));
		}
	}

	public static async Task SearchCallback(object sender, SearchResult result)
	{

		// AnsiConsole.MarkupLine($"[green]{result.Engine.Name}[/] | [link={result.RawUrl}]Raw[/]");

		var tx = new Table() { };

		var col = new TableColumn[]
		{
			new(nameof(SearchResultItem.Url))
			{
				Alignment = Justify.Center
			},
			new(nameof(SearchResultItem.Similarity))
			{
				Alignment = Justify.Center
			},
			new(nameof(SearchResultItem.Artist))
			{
				Alignment = Justify.Center
			},
			new(nameof(SearchResultItem.Character))
			{
				Alignment = Justify.Center
			},
			new(nameof(SearchResultItem.Source))
			{
				Alignment = Justify.Center
			},
			new(nameof(SearchResultItem.Description))
			{
				Alignment = Justify.Center
			},
			new("Dimensions")
			{
				Alignment = Justify.Center
			}

		};

		tx.AddColumns(col);

		foreach (SearchResultItem item in result.Results) {
			/*AnsiConsole.MarkupLine(
				$"\t[link={item.Url}]{item.Root.Engine.Name}[/] | {item.Similarity / 100:P} {item.Artist} " +
				$"{item.Description} [italic]{item.Title}[/] {item.Width}x{item.Height}");*/

			var row = new[]
			{
				$"[link={item.Url}]Link[/]",
				(($"{item.Similarity / 100:P}")),
				($"{item.Artist}").EscapeMarkup(),
				($"{item.Character}").EscapeMarkup(),
				($"{item.Source}").EscapeMarkup(),
				$"{item.Description}".EscapeMarkup(),
				$"{item.Width}x{item.Height}"
			};

			tx.AddRow(row);
		}

		var nameText = new Text(result.Engine.Name, Style.WithForeground(Color.Aqua))
		{
			Alignment = Justify.Center
		};

		var rawText = new Text("Raw", Style.WithLink(result.RawUrl))
		{
			Overflow  = Overflow.Ellipsis,
			Alignment = Justify.Center
		};

		Gui.ResultsTable.AddRow(nameText, rawText, tx);

		return;
	}
}