// Read S SmartImage.Linux SearchCommand.cs
// 2023-07-05 @ 2:07 AM

global using R2 = SmartImage.Linux.Resources;
global using R1 = SmartImage.Lib.Resources;
global using AC = Spectre.Console.AnsiConsole;
global using AConsole = Spectre.Console.AnsiConsole;
using System.ComponentModel;
using System.Data;
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

		[CommandOption("-e|--search-engines")]
		[DefaultValue(SearchConfig.SE_DEFAULT)]
		public SearchEngineOptions SearchEngines { get; init; }

		[CommandOption("-p|--priority-engines")]
		[DefaultValue(SearchConfig.PE_DEFAULT)]
		public SearchEngineOptions PriorityEngines { get; init; }

		[CommandOption("-a|--autosearch")]
		[DefaultValue(SearchConfig.AUTOSEARCH_DEFAULT)]
		public bool AutoSearch { get; init; }
	}

	public override ValidationResult Validate(CommandContext context, Settings settings)
	{

		var b = SearchQuery.IsValidSourceType(settings.Query);

		return b ? ValidationResult.Success() : ValidationResult.Error();
		// var v= base.Validate(context, settings);
		// return v;
	}

	public static Table FromDataTable(DataTable dt)
	{
		var t = new Table();

		foreach (DataColumn row in dt.Columns) {
			t.AddColumn(new TableColumn(row.ColumnName));
		}

		foreach (DataRow row in dt.Rows) {
			t.AddRow((string[]) row.ItemArray.Select(x => x.ToString()).ToArray());

		}

		return t;
	}

	public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
	{
		SearchMode? sm = null;

		await AConsole.Progress().AutoRefresh(true).StartAsync(async ctx =>
		{
			var p = ctx.AddTask("Creating query");
			p.IsIndeterminate = true;
			sm                = await SearchMode.TryCreateAsync(settings.Query);

			p.Increment(100);
			ctx.Refresh();

		});

		if (sm == null) {
			AConsole.WriteLine($"Error");
			return -1;
		}

		await sm.Client.ApplyConfigAsync();

		sm.Config.SearchEngines   = settings.SearchEngines;
		sm.Config.PriorityEngines = settings.PriorityEngines;
		sm.Config.AutoSearch      = settings.AutoSearch;

		var dt = sm.Config.ToTable();

		var t = FromDataTable(dt);

		AC.Write(t);

		var r = await sm.RunAsync(settings.Query);
		return 0;
	}
}