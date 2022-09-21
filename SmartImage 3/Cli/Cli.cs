using System.CommandLine;
using System.CommandLine.Help;
using Kantan.Text;
using SmartImage.Lib;
using Spectre.Console;

// ReSharper disable InconsistentNaming

namespace SmartImage.Cli;

public static class Cli
{
	public static readonly Option<SearchQuery> Opt_Query = new("-q", parseArgument: (ar) =>
	{
		return SearchQuery.TryCreateAsync(ar.Tokens.Single().Value).Result;

	}, isDefault: false, "Query (file or direct image URL)");

	public static readonly Option<string> Opt_Priority =
		new("-p", description: "Priority engines", getDefaultValue: () => SearchConfig.PE_DEFAULT.ToString());

	public static readonly Option<string> Opt_Engines = new(
		"-e", description: $"Search engines\n{Enum.GetValues<SearchEngineOptions>().QuickJoin("\n")}",
		getDefaultValue: () => SearchConfig.SE_DEFAULT.ToString());

	public static readonly Option<bool> Opt_OnTop = new(name: "-ontop", description: "Stay on top");

	public static readonly RootCommand Cmd_Root = new("Run a search")
	{
		Opt_Query,
		Opt_Priority,
		Opt_Engines,
		Opt_OnTop
	};

	public static void HelpHandler(HelpContext ctx)
	{
		ctx.HelpBuilder.CustomizeLayout(_ => HelpBuilder.Default.GetLayout()
		                                                .Skip(1) // Skip the default command description section.
		                                                .Prepend(_ => AnsiConsole.Write(
			                                                         new FigletText(Resources.Name))));
	}
}