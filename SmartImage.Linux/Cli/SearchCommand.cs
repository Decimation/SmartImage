// Read S SmartImage.Linux SearchCommand.cs
// 2023-07-05 @ 2:07 AM

global using R2 = SmartImage.Linux.Resources;
global using R1 = SmartImage.Lib.Resources;
global using AC = Spectre.Console.AnsiConsole;
global using AConsole = Spectre.Console.AnsiConsole;
using System.ComponentModel;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SmartImage.Linux.Cli;

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

	public override ValidationResult Validate(CommandContext context, Settings settings)
	{
		return base.Validate(context, settings);
	}

	public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
	{
		AppMode.Value.Config.SearchEngines = settings.Engines;
		AppMode.Value.Config.AutoSearch    = settings.AutoSearch;

		var r = await AppMode.Value.RunAsync(settings.Query);

		return 0;
	}
}