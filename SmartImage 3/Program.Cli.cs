using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using Kantan.Text;
using SmartImage.Lib;
using Spectre.Console;

// ReSharper disable InconsistentNaming

namespace SmartImage;

public static partial class Program
{
	/// <summary>
	/// <see cref="System.CommandLine"/>
	/// </summary>
	internal static class Cli
	{
		private static readonly Option<SearchQuery> Opt_Query = new("-q", parseArgument: ar =>
		{
			var value = ar.Tokens.Single().Value;
			var task  = SearchQuery.TryCreateAsync(value);
			return task.Result;

		}, isDefault: false, "Query (file or direct image URL)");

		private static readonly Option<string> Opt_Priority =
			new("-p", description: "Priority engines", getDefaultValue: () => SearchConfig.PE_DEFAULT.ToString());

		private static readonly Option<string> Opt_Engines = new(
			"-e", description: $"Search engines\n{Cache.EngineOptions.QuickJoin("\n")}",
			getDefaultValue: () => SearchConfig.SE_DEFAULT.ToString());

		private static readonly Option<bool> Opt_OnTop = new(name: "-ontop", description: "Stay on top");

		private static readonly RootCommand Cmd_Root = new("Run a search")
		{
			Opt_Query,
			Opt_Priority,
			Opt_Engines,
			Opt_OnTop
		};

		private static void HelpHandler(HelpContext ctx)
		{
			ctx.HelpBuilder.CustomizeLayout(_ => HelpBuilder.Default.GetLayout()
			                                                .Skip(1) // Skip the default command description section.
			                                                .Prepend(_ => AC.Write(
				                                                         new FigletText(Resources.Name))));
		}

		static Cli() { }

		internal static async Task<int> RunCli(string[] args)
		{
			Cmd_Root.SetHandler(async (t1, t2, t3, t4) =>
			{
				await SetQuery(t1);

				RootHandler(Enum.Parse<SearchEngineOptions>(t2), Enum.Parse<SearchEngineOptions>(t3), t4);
			}, Opt_Query, Opt_Engines, Opt_Priority, Opt_OnTop);

			var parser = new CommandLineBuilder(Cmd_Root).UseDefaults().UseHelp(HelpHandler).Build();

			var r = await parser.InvokeAsync(args);

			if (r != 0 || Query == null) {
				return r;
			}

			return r;
		}
	}
}