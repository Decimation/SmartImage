using Kantan.Net.Utilities;
using SmartImage.App;
using SmartImage.Lib;
using Spectre.Console;

// ReSharper disable InconsistentNaming

namespace SmartImage;

public static partial class Program
{
	/// <summary>
	/// <see cref="Spectre"/>
	/// </summary>
	internal static class Gui
	{
		#region Styles

		private static readonly Style S_Underline = Style.Parse("underline");

		internal static readonly Style S_Generic1 = new(foreground: Color.Blue);

		internal static readonly Style S_Generic2 = new(foreground: Color.Cyan1);

		#endregion

		internal static readonly TextPrompt<string> Pr_Input = new("Input:")
		{
			AllowEmpty = false,
			Validator = static s =>
			{
				try {

					var task  = SearchQuery.TryCreateAsync(s);
					var query = task.Result;
					Query = query;

					return ValidationResult.Success();
				}
				catch (Exception e) {
					return ValidationResult.Error($"Error: {e.Message}");
				}
			},
			PromptStyle = S_Underline,
		};

		internal static readonly MultiSelectionPrompt<SearchEngineOptions> Pr_Multi = new()
		{
			PageSize = 20,
		};

		internal static readonly MultiSelectionPrompt<SearchEngineOptions> Pr_Multi2 = new()
		{
			PageSize = 20,
		};

		internal static readonly TextPrompt<bool> Pr_Cfg_OnTop = new("Stay on top")
		{
			AllowEmpty       = true,
			ShowDefaultValue = true,
			PromptStyle      = S_Underline,
		};

		private static readonly SelectionPrompt<ResultMenuOption> Pr_ResultMenu = new();

		internal static readonly Table Tb_Results = new()
		{
			Border      = TableBorder.Heavy,
			BorderStyle = Style.Plain
		};

		private static readonly SelectionPrompt<MainMenuOption> Pr_Main = new()
		{
			Title    = "[underline]Main menu[/]",
			PageSize = 20,
		};

		private enum MainMenuOption
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

			Pr_Main = Pr_Main.AddChoices(Enum.GetValues<MainMenuOption>());

			Pr_Multi  = Pr_Multi.AddChoices(values);
			Pr_Multi2 = Pr_Multi2.AddChoices(values);

			Pr_Cfg_OnTop  = Pr_Cfg_OnTop.DefaultValue(SearchConfig.ON_TOP_DEFAULT);
			Pr_ResultMenu = Pr_ResultMenu.AddChoices(Enum.GetValues<ResultMenuOption>());

		}

		internal static async Task LiveCallback(LiveDisplayContext ctx)
		{
			Tb_Results.AddColumns("[bold]Engine[/]", "[bold]Info[/]", "[bold]Results[/]");

			while (!Status) {
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

				var url = item.Url?.ToString()?.EscapeMarkup();

				var row = new[]
				{
					$"[link={url}]Link[/]",
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

			Tb_Results.AddRow(text, caption, tx);
		}

		internal static async Task AfterSearch()
		{

			var p3 = new SelectionPrompt<int>();
			var c  = Enumerable.Range(0, Results.Count).ToList();

			const int i = -1;

			c.Insert(0, i);

			for (int j = 0; j < Results.Count; j++) {
				var range = Enumerable.Range(0, Results[j].Results.Count).ToList();
				range.Insert(0, i);
				p3 = p3.AddChoiceGroup(j, range);

			}

			switch (AC.Prompt(Pr_ResultMenu)) {

				case ResultMenuOption.Stay:

					int l;

					do {
						l = AC.Prompt(p3);

						if (l == i) {
							break;
						}

						var r = Results[l];

						if (r.First is { Url: { } }) {
							HttpUtilities.OpenUrl(r.First.Url);
						}

					} while (true);

					break;
				case ResultMenuOption.Exit:
					Environment.Exit(0);
					break;
				default:
					Environment.Exit(-1);
					break;
			}
		}

		internal static async Task RunGui()
		{
			MAIN_MENU:
			var opt = AC.Prompt(Pr_Main);

			switch (opt) {
				case MainMenuOption.Search:
					var q  = AC.Prompt(Pr_Input);
					var t2 = AC.Prompt(Pr_Multi.Title("Engines"));
					var t3 = AC.Prompt(Pr_Multi2.Title("Priority engines"));
					var t4 = AC.Prompt(Pr_Cfg_OnTop);

					SearchEngineOptions a = t2.Aggregate(SearchEngineOptions.None, Cache.EnumAggregator);
					SearchEngineOptions b = t3.Aggregate(SearchEngineOptions.None, Cache.EnumAggregator);

					await RootHandler(Query, a.ToString(), b.ToString(), t4);

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

		public static readonly FigletText NameFiglet = new FigletText(font: FigletFont.Default, text: Resources.Name);
	}
}