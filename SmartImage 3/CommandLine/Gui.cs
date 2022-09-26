using Kantan.Net.Utilities;
using SmartImage.App;
using SmartImage.Lib;
using Spectre.Console;

namespace SmartImage.CommandLine;

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
		PageSize = 20,
	};

	internal static readonly MultiSelectionPrompt<SearchEngineOptions> Prompt3 = new()
	{
		Title    = "Priority engines:",
		PageSize = 20,
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
		PageSize = 20,
	};

	internal enum MainMenuOption
	{
		Search,
		Options
	}

	private enum ResultMenuOption
	{
		Stay,
		Exit
	}

	static Gui()
	{
		var values = Cache.EngineOptions;

		MainPrompt = MainPrompt.AddChoices(Enum.GetValues<MainMenuOption>());

		Prompt2 = Prompt2.AddChoices(values);
		Prompt3 = Prompt3.AddChoices(values);
		Prompt4 = Prompt4.DefaultValue(SearchConfig.ON_TOP_DEFAULT);
	}

	internal static async Task LiveCallback(LiveDisplayContext ctx)
	{
		ResultsTable.AddColumns("[bold]Engine[/]", "[bold]Info[/]", "[bold]Results[/]");

		while (!Program.Status) {
			ctx.Refresh();
			await Task.Delay(TimeSpan.FromMilliseconds(100));

		}
	}

	internal static async Task SearchCallback(object sender, SearchResult result)
	{
		var text = new Text($"{result.Engine.Name}", style: new Style(decoration: Decoration.Bold));

		var caption = new Text("Raw", new Style(link: result.RawUrl, decoration: Decoration.Italic));

		var tx = new Table
		{
			Alignment = Justify.Center,
			Border    = TableBorder.Heavy
		};

		var col = new TableColumn[]
		{
			new($"[bold]{nameof(SearchResultItem.Url)}[/]")
			{
				Alignment = Justify.Center
			},
			new($"[bold]{nameof(SearchResultItem.Similarity)}[/]")
			{
				Alignment = Justify.Center
			},
			new($"[bold]{nameof(SearchResultItem.Artist)}[/]")
			{
				Alignment = Justify.Center,
			},
			new($"[bold]{nameof(SearchResultItem.Character)}[/]")
			{
				Alignment = Justify.Center
			},
			new($"[bold]{nameof(SearchResultItem.Source)}[/]")
			{
				Alignment = Justify.Center
			},
			new($"[bold]{nameof(SearchResultItem.Description)}[/]")
			{
				Alignment = Justify.Center
			},
			new("[bold]Dimensions[/]")
			{
				Alignment = Justify.Center
			}

		};

		tx.AddColumns(col);

		foreach (SearchResultItem item in result.Results) {
			/*AC.MarkupLine(
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

		// AC.Write(tx);

		ResultsTable.AddRow(text, caption, tx);
	}

	internal static async Task AfterSearch()
	{
		var p2 = new SelectionPrompt<ResultMenuOption>();
		p2 = p2.AddChoices(Enum.GetValues<ResultMenuOption>());

		var p3 = new SelectionPrompt<int>();
		var c  = Enumerable.Range(0, Program.Results.Count).ToList();

		const int i = -1;

		c.Insert(0, i);
		p3 = p3.AddChoices(c);

		switch (AC.Prompt(p2)) {

			case ResultMenuOption.Stay:

				int l;

				do {
					l = AC.Prompt(p3);

					if (l == i) {
						break;
					}

					var r = Program.Results[l];
					HttpUtilities.OpenUrl(r.First?.Url);
				} while (true);

				break;
			case ResultMenuOption.Exit:
				Environment.Exit(0);
				break;
		}
	}

	internal static async Task RunGui()
	{
		MAIN_MENU:
		var opt = AC.Prompt(MainPrompt);

		switch (opt) {
			case MainMenuOption.Search:
				var q  = AC.Prompt(Prompt);
				var t2 = AC.Prompt(Prompt2);
				var t3 = AC.Prompt(Prompt3);
				var t4 = AC.Prompt(Prompt4);

				SearchEngineOptions a = t2.Aggregate(SearchEngineOptions.None, Cache.EnumAggregator);
				SearchEngineOptions b = t3.Aggregate(SearchEngineOptions.None, Cache.EnumAggregator);

				await Program.RootHandler(Program.Query, a.ToString(), b.ToString(), t4);

				break;
			case MainMenuOption.Options:
				SelectionPrompt<bool> ctx = new();
				ctx = ctx.AddChoices(true, false);

				var v = AC.Prompt(ctx);

				AC.MarkupLine($"{Integration.ExeLocation}\n" +
				                       $"{Integration.IsAppFolderInPath}\n" +
				                       $"{Integration.IsContextMenuAdded}");

				goto MAIN_MENU;
		}
	}
}