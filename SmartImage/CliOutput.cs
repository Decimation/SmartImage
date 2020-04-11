using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using SmartImage.Model;

namespace SmartImage
{
	public static class CliOutput
	{
		public delegate void RunCommand(string[] args);

		private const string STRING_FORMAT_ARG = "msg";

		private const char HEAVY_BALLOT_X   = '\u2718';
		private const char HEAVY_CHECK_MARK = '\u2714';

		public const char MUL_SIGN = '\u00D7';
		public const char RAD_SIGN = '\u221A';

		private const char GT = '>';

		private const string DEBUG_COND = "DEBUG";


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


		[StringFormatMethod(STRING_FORMAT_ARG)]
		internal static void OnCurrentLine(ConsoleColor color, string s)
		{
			var clear = new string('\b', s.Length);
			Console.Write(clear);

			WithColor(color, () =>
			{
				//
				Console.Write(s);
			});
		}

		[Conditional(DEBUG_COND)]
		[StringFormatMethod(STRING_FORMAT_ARG)]
		public static void WriteDebug(string msg, params object[] args)
		{
			WithColor(ConsoleColor.Cyan, () =>
			{
				//
				Console.WriteLine("{0} {1}", MUL_SIGN, string.Format(msg, args));
			});
		}

		public static void WriteHelp()
		{
			Console.WriteLine("Available commands:\n");

			foreach (var command in AllCommands) {
				Console.WriteLine(command);
				Console.WriteLine();
			}

			Console.WriteLine("See readme: {0}", Config.Readme);
		}


		[StringFormatMethod(STRING_FORMAT_ARG)]
		public static bool ReadConfirm(string msg, params object[] args)
		{
			Console.Clear();
			CliOutput.WriteInfo("{0} (y/n)", string.Format(msg, args));

			Console.WriteLine();


			char key = char.ToLower(Console.ReadKey().KeyChar);

			Console.WriteLine();

			if (key == 'n') {
				return false;
			}
			else if (key == 'y') {
				return true;
			}
			else {
				return ReadConfirm(msg, args);
			}
		}

		public static CliCommand ReadCommand(string s)
		{
			var cmd = AllCommands.FirstOrDefault(cliCmd => cliCmd.Parameter == s);

			return cmd;
		}

		public static void Init()
		{
			Console.Title          = Config.NAME;
			Console.OutputEncoding = Encoding.Unicode;
			Console.Clear();
		}

		public static char Bool(bool b) => b ? RAD_SIGN : MUL_SIGN;

		public static void WithColor(ConsoleColor color, Action func)
		{
			var oldColor = Console.ForegroundColor;
			Console.ForegroundColor = color;
			func();
			Console.ForegroundColor = oldColor;
		}

		[StringFormatMethod(STRING_FORMAT_ARG)]
		public static void WriteError(string msg, params object[] args)
		{
			WithColor(ConsoleColor.Red, () =>
			{
				//
				Console.WriteLine("{0} {1}", MUL_SIGN, string.Format(msg, args));
			});
		}

		[StringFormatMethod(STRING_FORMAT_ARG)]
		public static void WriteInfo(string msg, params object[] args)
		{
			WithColor(ConsoleColor.White, () =>
			{
				//
				Console.WriteLine("{0} {1}", GT, string.Format(msg, args));
			});
		}

		[StringFormatMethod(STRING_FORMAT_ARG)]
		public static void WriteSuccess(string msg, params object[] args)
		{
			WithColor(ConsoleColor.Green, () =>
			{
				//
				Console.WriteLine("{0} {1}", RAD_SIGN, string.Format(msg, args));
			});
		}
	}
}