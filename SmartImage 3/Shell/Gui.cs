using SmartImage.Lib;
using Spectre.Console;

namespace SmartImage.Shell;

/// <summary>
/// <see cref="Spectre"/>
/// </summary>
internal static class Gui
{
	private static readonly Style PromptStyle = Style.Parse("underline");

	internal static readonly TextPrompt<string> Prompt = new("Input:")
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

	internal static readonly MultiSelectionPrompt<SearchEngineOptions> Prompt2 = new()
	{
		Title    = "Engines:",
		PageSize = 15,
	};

	internal static readonly MultiSelectionPrompt<SearchEngineOptions> Prompt3 = new()
	{
		Title    = "Priority engines:",
		PageSize = 15,
	};

	internal static readonly TextPrompt<bool> Prompt4 = new("Stay on top")
	{
		AllowEmpty       = true,
		ShowDefaultValue = true,
		PromptStyle      = PromptStyle,
	};

	internal static readonly Table ResultsTable = new()
	{
		Border      = TableBorder.Heavy,
		BorderStyle = Style.Plain
	};

	internal static readonly SelectionPrompt<MainMenuOption> MainPrompt = new()
	{
		Title    = "[underline]Main menu[/]",
		PageSize = 15,
	};

	internal enum MainMenuOption
	{
		Search,
		Options
	}

	static Gui()
	{
		var values = Enum.GetValues<SearchEngineOptions>();

		MainPrompt = MainPrompt.AddChoices(Enum.GetValues<MainMenuOption>());

		Prompt2 = Prompt2.AddChoices(values);
		Prompt3 = Prompt3.AddChoices(values);
		Prompt4 = Prompt4.DefaultValue(SearchConfig.ON_TOP_DEFAULT);
	}

	internal static async Task LiveCallback(LiveDisplayContext ctx)
	{
		ResultsTable.AddColumns("[bold]Engine[/]", "[bold]Info[/]", nameof(SearchResult.Results));

		while (!Program.Status) {
			ctx.Refresh();
			await Task.Delay(TimeSpan.FromMilliseconds(100));

		}
	}

	internal static async Task SearchCallback(object sender, SearchResult result)
	{
		var tx = new Table();

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
				$"{item.Similarity / 100:P}",
				$"{item.Artist}".EscapeMarkup(),
				$"{item.Character}".EscapeMarkup(),
				$"{item.Source}".EscapeMarkup(),
				$"{item.Description}".EscapeMarkup(),
				$"{item.Width}x{item.Height}"
			};

			tx.AddRow(row);
		}

		var nameText = new Text(result.Engine.Name, new Style(foreground: Color.Aqua, decoration: Decoration.Bold))
		{
			Alignment = Justify.Center
		};

		var rawText = new Text("Raw", new Style(link: result.RawUrl.ToString()))
		{
			Overflow  = Overflow.Ellipsis,
			Alignment = Justify.Center
		};

		ResultsTable.AddRow(nameText, rawText, tx);

	}
}