using System;
using Neocmd;
using SmartImage.Engines;
using SmartImage.Searching;
using SmartImage.Utilities;

namespace SmartImage
{
	public static class Commands
	{
		private static readonly CliCommand SetImgurAuth = new CliCommand
		{
			Parameter   = "--set-imgur-auth",
			Syntax      = "<consumer id> <consumer secret>",
			Description = "Sets up Imgur API authentication",
			Action = args =>
			{
				var newId     = args[1];
				var newSecret = args[2];

				Console.WriteLine("New client ID and secret: ({0}, {1})", newId, newSecret);

				Config.ImgurAuth = new AuthInfo(newId, newSecret);
			}
		};

		private static readonly CliCommand SetSauceNaoAuth = new CliCommand
		{
			Parameter   = "--set-saucenao-auth",
			Syntax      = "<api key>",
			Description = "Sets up SauceNao API authentication",
			Action = args =>
			{
				var newKey = args[1];

				Console.WriteLine("New API key: {0}", newKey);

				Config.SauceNaoAuth = new AuthInfo(newKey, null);
			}
		};

		private static readonly CliCommand SetSearchEngines = new CliCommand
		{
			Parameter   = "--search-engines",
			Syntax      = "<search engines>",
			Description = "Sets the search engines to utilize when searching; delimited by commas",
			Action = args =>
			{
				var newOptions = args[1];

				Console.WriteLine("Engines: {0}", newOptions);

				Config.SearchEngines = Enum.Parse<SearchEngines>(newOptions);
			}
		};

		private static readonly CliCommand SetPriorityEngines = new CliCommand
		{
			Parameter = "--priority-engines",
			Syntax    = "<search engines>",
			Description = "Sets the search engines whose results to automatically " +
			              "open in the browser when search is complete; delimited by commas",
			Action = args =>
			{
				var newOptions = args[1];

				Console.WriteLine("Priority engines: {0}", newOptions);

				Config.PriorityEngines = Enum.Parse<SearchEngines>(newOptions);
			}
		};

		private static readonly CliCommand ContextMenu = new CliCommand
		{
			Parameter   = "--ctx-menu",
			Syntax      = null,
			Description = "Installs the context menu integration",
			Action = args =>
			{
				Config.AddToContextMenu();

				Console.WriteLine("Added to context menu!");
			}
		};

		private static readonly CliCommand AddToPath = new CliCommand
		{
			Parameter   = "--add-to-path",
			Syntax      = null,
			Description = "Adds executable path to path environment variable",
			Action      = args => { Config.AddToPath(); }
		};

		private static readonly CliCommand Reset = new CliCommand()
		{
			Parameter   = "--reset",
			Syntax      = "[all]",
			Description = "Resets configuration to defaults. Specify <all> to fully reset.",
			Action = args =>
			{
				bool all = false;

				if (args.Length >= 2) {
					all = args[1] == "all";
				}

				Config.Reset(all);

				Console.WriteLine("Config reset");
			}
		};

		private static readonly CliCommand Info = new CliCommand()
		{
			Parameter   = "--info",
			Syntax      = null,
			Description = "Information about the program",
			Action      = args => { Config.Info(); }
		};

		
		public static readonly CliCommand[] AllCommands =
		{
			SetImgurAuth, SetSauceNaoAuth, SetSearchEngines, SetPriorityEngines,
			ContextMenu, Reset, AddToPath, Info
		};

		public static void Setup()
		{
			CliOutput.Commands.AddRange(AllCommands);
			CliOutput.Init(Config.NAME);
			Config.Check();
		}
	}
}