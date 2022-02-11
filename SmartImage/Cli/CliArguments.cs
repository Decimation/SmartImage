using Kantan.Utilities;
using SmartImage.Lib.Searching;

namespace SmartImage.Cli;

internal static class CliArguments
{
	/// <summary>
	/// Command line argument handler
	/// </summary>
	internal static readonly CliHandler ArgumentHandler = new()
	{
		Parameters =
		{
			new()
			{
				ArgumentCount = 1,
				ParameterId   = "-se",
				Function = strings =>
				{
					Program.Config.SearchEngines = Enum.Parse<SearchEngineOptions>(strings[0]);
					return null;
				}
			},
			new()
			{
				ArgumentCount = 1,
				ParameterId   = "-pe",
				Function = strings =>
				{
					Program.Config.PriorityEngines = Enum.Parse<SearchEngineOptions>(strings[0]);
					return null;
				}
			},
			new()
			{
				ArgumentCount = 0,
				ParameterId   = "-f",
				Function = delegate
				{
					Program.Config.Filtering = true;
					return null;
				}
			},
			new()
			{
				ArgumentCount = 0,
				ParameterId   = "-output_only",
				Function = delegate
				{
					Program.Config.OutputOnly = true;
					return null;
				}
			}
		},
		Default = new()
		{
			ArgumentCount = 1,
			ParameterId   = null,
			Function = strings =>
			{
				Program.Config.Query = strings[0];
				return null;
			}
		}
	};
}