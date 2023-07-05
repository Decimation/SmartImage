global using R2 = SmartImage.Linux.Resources;
global using R1 = SmartImage.Lib.Resources;
global using AC = Spectre.Console.AnsiConsole;
using System.ComponentModel;
using JetBrains.Annotations;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Results;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SmartImage.Linux;

public static class Program
{
	public static async Task<int> Main(string[] args)
	{

		AC.Write(new FigletText(R1.Name)
			         .LeftJustified()
			         .Color(Color.Red));

		if (args.Any()) {
			var e = args.GetEnumerator();

		}

		var app = new CommandApp<SearchCommand>();
		return await app.RunAsync(args);
	}

	internal sealed class SearchCommand : AsyncCommand<SearchCommand.Settings>
	{
		public sealed class Settings : CommandSettings
		{
			[Description("Query")]
			[CommandArgument(0, "[query]")]
			public string? Query { get; init; }

			[CommandOption("-e|--engines")]
			[DefaultValue(SearchConfig.SE_DEFAULT)]
			public SearchEngineOptions Engines { get; init; }

			[CommandOption("--autosearch")]
			[DefaultValue(SearchConfig.AUTOSEARCH_DEFAULT)]
			public bool AutoSearch { get; init; }
		}

		public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
		{
			var cfg = new SearchConfig()
			{
				SearchEngines = settings.Engines,
				AutoSearch    = settings.AutoSearch
			};

			var sc = new SearchClient(cfg);
			var sq = await SearchQuery.TryCreateAsync(settings.Query);
			await sq.UploadAsync();
			AC.WriteLine($"{sq}");
			var r = await sc.RunSearchAsync(sq);

			foreach (var sr in r) {
				AC.WriteLine($"{sr}");

				foreach (SearchResultItem sri in sr.AllResults) {
					AC.WriteLine($"{sri}");

				}
			}

			return 0;
		}
	}
}