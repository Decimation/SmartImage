#region

using System;
using Neocmd;
using SmartImage.Engines.SauceNao;
using SmartImage.Model;
using SmartImage.Searching;
using SmartImage.Utilities;

#endregion

namespace SmartImage
{
	public static class Commands
	{
		private static readonly CliCommand SetImgurAuth = new CliCommand
		{
			Parameter   = "--set-imgur-auth",
			Syntax      = "<consumer id>",
			Description = "Sets up Imgur API authentication",
			Action = args =>
			{
				string newId = args[1];

				CliOutput.WriteInfo("New client ID and secret: {0}", newId);

				Config.ImgurAuth = new AuthInfo(newId);
			}
		};

		private static readonly CliCommand SetSauceNaoAuth = new CliCommand
		{
			Parameter   = "--set-saucenao-auth",
			Syntax      = "<api key>",
			Description = "Sets up SauceNao API authentication",
			Action = args =>
			{
				string newKey = args[1];

				CliOutput.WriteInfo("New API key: {0}", newKey);

				Config.SauceNaoAuth = new AuthInfo(newKey);
			}
		};

		private static readonly CliCommand SetSearchEngines = new CliCommand
		{
			Parameter   = "--search-engines",
			Syntax      = "<search engines>",
			Description = "Sets the search engines to utilize when searching; delimited by commas",
			Action = args =>
			{
				string newOptions = args[1];

				CliOutput.WriteInfo("Engines: {0}", newOptions);

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
				string newOptions = args[1];

				CliOutput.WriteInfo("Priority engines: {0}", newOptions);

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

				CliOutput.WriteInfo("Added to context menu!");
			}
		};

		private static readonly CliCommand AddToPath = new CliCommand
		{
			Parameter   = "--add-to-path",
			Syntax      = null,
			Description = "Adds executable path to path environment variable",
			Action      = args => { Config.AddToPath(); }
		};

		private static readonly CliCommand Reset = new CliCommand
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


				CliOutput.WriteInfo("Config reset");
			}
		};

		private static readonly CliCommand Info = new CliCommand
		{
			Parameter   = "--info",
			Syntax      = null,
			Description = "Information about the program",
			Action = args =>
			{
				//
				Config.Info();
			}
		};

		private static readonly CliCommand CreateSauceNaoAcc = new CliCommand
		{
			Parameter   = "--create-saucenao",
			Syntax      = "[auto]",
			Description = "Create SauceNao account (for API keys)",
			Action = args =>
			{
				bool auto = false;

				if (args.Length >= 2) {
					auto = args[1] == "auto";
				}
				
				// todo
				var acc = SauceNao.CreateAccount(auto);

				if (!acc.IsNull) {
					Config.SauceNaoAuth = new AuthInfo(acc.ApiKey);
				}
			}
		};

		private static readonly CliCommand Help = new CliCommand
		{
			Parameter   = "--help",
			Syntax      = null,
			Description = "Display available commands",
			Action = args =>
			{
				CliOutput.WriteCommands();
				CliOutput.WriteInfo("Readme: {0}", Config.Readme);
			}
		};

		public static readonly CliCommand[] AllCommands =
		{
			SetImgurAuth, SetSauceNaoAuth, SetSearchEngines, SetPriorityEngines,
			ContextMenu, Reset, AddToPath, Info, Help, CreateSauceNaoAcc
		};

		public static void Setup()
		{
			CliOutput.Commands.AddRange(AllCommands);
			CliOutput.Init(Config.NAME, false);
		}
	}
}