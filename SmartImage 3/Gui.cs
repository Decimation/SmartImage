using System.Diagnostics;
using Kantan.Console;
using Kantan.Net.Utilities;
using Novus.Win32;
using SmartImage.App;
using SmartImage.Lib;
using Spectre.Console;

// ReSharper disable InconsistentNaming

namespace SmartImage;

/// <summary>
/// <see cref="Spectre"/>
/// </summary>
internal class Gui : ProgramMode
{
	#region Styles

	private static readonly Style S_Underline = Style.Parse("underline");

	internal static readonly Style S_Generic1 = new(foreground: Color.Blue);

	internal static readonly Style S_Generic2 = new(foreground: Color.Cyan1);

	#endregion

	#region Widgets

	internal TextPrompt<string> Pr_Input = new(Resources.S_Input)
	{
		AllowEmpty = false,

		PromptStyle = S_Underline,
	};

	internal readonly MultiSelectionPrompt<SearchEngineOptions> Pr_Multi = new()
	{
		PageSize = 20,
	};

	internal readonly MultiSelectionPrompt<SearchEngineOptions> Pr_Multi2 = new()
	{
		PageSize = 20,
	};

	internal readonly TextPrompt<bool> Pr_Cfg_OnTop = new(Resources.S_OnTop)
	{
		AllowEmpty       = true,
		ShowDefaultValue = true,
		PromptStyle      = S_Underline,
	};

	private readonly SelectionPrompt<ResultMenuOption> Pr_ResultMenu = new();

	internal readonly Table Tb_Results = new()
	{
		Border      = TableBorder.Heavy,
		BorderStyle = Style.Plain
	};

	private readonly SelectionPrompt<MainMenuOption> Pr_Main = new()
	{
		Title    = "[underline]Main menu[/]",
		PageSize = 20,
	};

	internal readonly FigletText NameFiglet = new(font: FigletFont.Default, text: Resources.Name);

	private Task m_live1;

	#endregion

	private enum MainMenuOption
	{
		Search,
		Options,
		Clipboard
	}

	private enum ResultMenuOption
	{
		Stay,
		Exit
	}

	static Gui() { }

	internal async Task LiveCallback(LiveDisplayContext ctx)
	{
		Tb_Results.AddColumns("[bold]Engine[/]", "[bold]Info[/]", "[bold]Results[/]");
		Tb_Results.Alignment = Justify.Center;

		while (!Status) {
			ctx.Refresh();
			await Task.Delay(TimeSpan.FromMilliseconds(100));

		}
	}

	private async Task HandleQueryAsync(SearchQuery t1)
	{
		Query = t1;

		var t = AC.Status().Spinner(Spinner.Known.Star)
		          .StartAsync($"Uploading...", async ctx =>
		          {
			          await Query.UploadAsync();
			          ctx.Status = "Uploaded";
		          });

		await t;
	}

	#region Overrides of ProgramMode<object>

	public override async Task<object> Run(SearchClient c, string[] args)
	{
		MAIN_MENU:
		var opt = AC.Prompt(Pr_Main);

		switch (opt) {
			case MainMenuOption.Clipboard:
				Pr_Input = Pr_Input.DefaultValue(Cache.Clipboard.Value);
				goto case MainMenuOption.Search;
			case MainMenuOption.Search:

				var q  = AC.Prompt(Pr_Input);
				var t2 = AC.Prompt(Pr_Multi.Title("Engines").NotRequired());
				var t3 = AC.Prompt(Pr_Multi2.Title("Priority engines").NotRequired());
				var t4 = AC.Prompt(Pr_Cfg_OnTop);

				SearchEngineOptions a = t2.Aggregate(SearchEngineOptions.None, Cache.EnumAggregator);
				SearchEngineOptions b = t3.Aggregate(SearchEngineOptions.None, Cache.EnumAggregator);

				await HandleQueryAsync(Query);

				RootHandler(a, b, t4);

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

		return this;
	}

	#region Overrides of ProgramMode

	#endregion

	public Gui(SearchQuery q) : base(q) { }

	public Gui()
	{
		var values = Cache.EngineOptions;

		Pr_Input.Validator = s =>
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
		};

		Pr_Main = Pr_Main.AddChoices(Enum.GetValues<MainMenuOption>());

		Pr_Multi      = Pr_Multi.AddChoices(values);
		Pr_Multi2     = Pr_Multi2.AddChoices(values);
		Pr_Cfg_OnTop  = Pr_Cfg_OnTop.DefaultValue(SearchConfig.ON_TOP_DEFAULT);
		Pr_ResultMenu = Pr_ResultMenu.AddChoices(Enum.GetValues<ResultMenuOption>());
	}

	public override async Task PreSearch(SearchConfig c, object? sender)
	{
		var table = new Table()
		{
			Border    = TableBorder.Heavy,
			Alignment = Justify.Center
		};

		//NOTE: WTF
		table.AddColumns(new TableColumn("Input".T()), new TableColumn("Value".T()))
		     .AddRow(new Text(Resources.S_SearchEngines, Gui.S_Generic1),
		             new Text(c.SearchEngines.ToString(), Gui.S_Generic2))
		     .AddRow(new Text(Resources.S_PriorityEngines, Gui.S_Generic1),
		             new Text(c.PriorityEngines.ToString(), Gui.S_Generic2))
		     .AddRow(new Text(Resources.S_OnTop, Gui.S_Generic1), new Text(c.OnTop.ToString(), Gui.S_Generic2))
		     .AddRow(new Text("Query input", Gui.S_Generic1), new Text(Query.Value, Gui.S_Generic2))
		     .AddRow(new Text("Query upload", Gui.S_Generic1), new Text(Query.Upload.ToString(), Gui.S_Generic2));

		AC.Write(table);

		m_live1 = AC.Live(Tb_Results)
		            .AutoClear(false)
		            .Overflow(VerticalOverflow.Ellipsis)
		            .Cropping(VerticalOverflowCropping.Top)
		            .StartAsync(LiveCallback);
	}

	public override async Task PostSearch(SearchConfig c, object? sender, List<SearchResult> results1)
	{
		var now = sender as Stopwatch;

		now.Stop();

		var diff = now.Elapsed;

		AC.WriteLine($"Completed in ~{diff.TotalSeconds:F}");

		var p3 = new SelectionPrompt<int>();

		var r = Enumerable.Range(0, results1.Count).ToList();

		const int i = -1;

		r.Insert(0, i);

		for (int j = 0; j < results1.Count; j++) {
			var range = Enumerable.Range(0, results1[j].Results.Count).ToList();
			// range.Insert(0, i);

			int j1 = j;

			p3 = p3.UseConverter(i1 =>
			{

				return i1.ToString();
			}).AddChoiceGroup(j, range).UseConverter(i2 =>
			{

				return i2.ToString();
			});

		}

		p3.AddChoice(i);

		switch (AC.Prompt(Pr_ResultMenu)) {

			case ResultMenuOption.Stay:

				int l;

				do {
					l = AC.Prompt(p3);

					if (l == i) {
						break;
					}

					if (l >= 0 && l < results1.Count) {
						var rx = results1[l];

						if (rx.First is { Url: { } }) {
							HttpUtilities.OpenUrl(rx.First.Url);
						}
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

	public override async Task OnResult(object o, SearchResult result)
	{
		var bg = result.Status switch
		{
			SearchResultStatus.Unavailable => Color.Yellow4,
			SearchResultStatus.NoResults   => Color.Grey,
			SearchResultStatus.Extraneous  => Color.Orange3,
			SearchResultStatus.Cooldown    => Color.Orange1,
			SearchResultStatus.Failure     => Color.Red,
			SearchResultStatus.Success     => Color.Green,
			_                              => Color.Grey,
		};

		var text = new Text($"{result.Engine.Name}", style: new Style(decoration: Decoration.Bold, background: bg));

		var caption = new Text("Raw", new Style(link: result.RawUrl, decoration: Decoration.Italic));

		var tx = new Table
		{
			Alignment = Justify.Center,
			Border    = TableBorder.Heavy
		};

		var col = new TableColumn[]
		{
			new($"[bold]#[/]")
			{
				// Alignment = Justify.Center
			},
			new($"[bold]{nameof(SearchResultItem.Url)}[/]")
			{
				// Alignment = Justify.Center
			},
			new($"[bold]{nameof(SearchResultItem.Similarity)}[/]")
			{
				// Alignment = Justify.Center
			},
			new($"[bold]{nameof(SearchResultItem.Artist)}[/]")
			{
				// Alignment = Justify.Center,
			},
			new($"[bold]{nameof(SearchResultItem.Character)}[/]")
			{
				// Alignment = Justify.Center
			},
			new($"[bold]{nameof(SearchResultItem.Source)}[/]")
			{
				// Alignment = Justify.Center
			},
			new($"[bold]{nameof(SearchResultItem.Description)}[/]")
			{
				// Alignment = Justify.Center
			},
			new($"[bold]{nameof(SearchResultItem.Title)}[/]")
			{
				// Alignment = Justify.Center
			},
			new($"[bold]{nameof(SearchResultItem.Time)}[/]")
			{
				// Alignment = Justify.Center
			},
			new("[bold]Dimensions[/]")
			{
				// Alignment = Justify.Center
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
				$"{result.Results.IndexOf(item)}",
				$"[link={url}]Link[/]",
				$"{item.Similarity / 100:P}",
				$"{item.Artist}".EscapeMarkup(),
				$"{item.Character}".EscapeMarkup(),
				$"{item.Source}".EscapeMarkup(),
				$"{item.Description}".EscapeMarkup(),
				$"{item.Title}".EscapeMarkup(),
				$"{item.Time}".EscapeMarkup(),
				$"{item.Width}x{item.Height}"
			};

			tx.AddRow(row);
		}

		// AC.Write(tx);
		tx = tx.RemoveEmpty();
		Tb_Results.AddRow(text, caption, tx);
	}

	public override async Task OnComplete(object sender, List<SearchResult> e)
	{
		Native.FlashWindow(Cache.HndWindow);

	}

	#region Overrides of ProgramMode

	public override async Task Close() { }

	public override void Dispose() { }

	#endregion

	#endregion
}