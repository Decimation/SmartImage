using System;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace SmartImage
{
	public static class Cli
	{
		public delegate void RunCommand(string[] args);


		public static readonly CliCommand Setup = new CliCommand
		{
			Parameter   = "--setup",
			Syntax      = "<consumer id> <consumer secret>",
			Description = "Sets up Imgur API authentication",
			Action = args =>
			{
				var newId     = args[1];
				var newSecret = args[2];

				Console.WriteLine("New client ID and secret: ({0}, {1})", newId, newSecret);

				Config.ImgurAuth = (newId, newSecret);
			}
		};

		public static readonly CliCommand SetOpenOptions = new CliCommand
		{
			Parameter   = "--open-options",
			Syntax      = "<options>",
			Description = "Sets the behavior for opening results when a match is found; delimited by commas",
			Action = args =>
			{
				var newOptions = args[1];

				Console.WriteLine("New options: {0}", newOptions);

				Config.OpenOptions = Enum.Parse<OpenOptions>(newOptions);
			}
		};

		public static readonly CliCommand ContextMenu = new CliCommand
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

		public static readonly CliCommand[] AllCommands = new[] {Setup, SetOpenOptions, ContextMenu};

		public static void Help()
		{
			Console.WriteLine("Available commands:\n");

			foreach (var command in AllCommands) {
				Console.WriteLine(command);
				Console.WriteLine();
			}
		}

		public static CliCommand ReadCommand(string s)
		{
			var cmd = AllCommands.FirstOrDefault(cliCmd => cliCmd.Parameter == s);

			if (cmd == null) {
				//Console.WriteLine("Command not found: \"{0}\"", s);
				//Help();
				return null;
			}

			return cmd;
		}

		private const string STRING_FORMAT_ARG = "msg";

		private const char HEAVY_BALLOT_X   = '\u2718';
		private const char HEAVY_CHECK_MARK = '\u2714';

		private const char MUL_SIGN = '\u00D7';
		private const char RAD_SIGN = '\u221A';

		private const char GT = '>';

		public static void Init()
		{
			Console.Title          = "SmartImage";
			Console.OutputEncoding = Encoding.Unicode;
			Console.Clear();
		}

		[StringFormatMethod(STRING_FORMAT_ARG)]
		public static void Error(string msg, params object[] args)
		{
			var oldColor = Console.ForegroundColor;

			Console.ForegroundColor = ConsoleColor.Red;

			Console.WriteLine("{0} {1}", MUL_SIGN, string.Format(msg, args));

			Console.ForegroundColor = oldColor;
		}

		[StringFormatMethod(STRING_FORMAT_ARG)]
		public static void Info(string msg, params object[] args)
		{
			var oldColor = Console.ForegroundColor;

			Console.ForegroundColor = ConsoleColor.White;

			Console.WriteLine("{0} {1}", GT, string.Format(msg, args));

			Console.ForegroundColor = oldColor;
		}

		[StringFormatMethod(STRING_FORMAT_ARG)]
		public static void Success(string msg, params object[] args)
		{
			var oldColor = Console.ForegroundColor;

			Console.ForegroundColor = ConsoleColor.Green;

			Console.WriteLine("{0} {1}", RAD_SIGN, string.Format(msg, args));

			Console.ForegroundColor = oldColor;
		}
	}
}