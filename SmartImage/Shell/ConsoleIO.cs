using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using SimpleCore.Win32;
using SimpleCore.Win32.Cli;
using SmartImage.Utilities;

// ReSharper disable InconsistentNaming

// ReSharper disable UseStringInterpolation

// ReSharper disable ParameterTypeCanBeEnumerable.Global

namespace SmartImage.Shell
{
	/// <summary>
	/// Program functionality, IO, console interaction, console UI
	/// </summary>
	internal static class ConsoleIO
	{
		// todo: add ANSI sequence features like bold?

		internal const char CLI_CHAR = '*';

		internal const string ALT_DENOTE = "[Alt]";

		internal static string GetInput(string prompt)
		{
			Console.Write("{0}: ", prompt);
			var i = Console.ReadLine();

			return String.IsNullOrWhiteSpace(i) ? null : i;

		}


		internal static void WaitForInput()
		{
			Console.WriteLine();
			Console.WriteLine("Press any key to continue...");
			Console.ReadLine();
		}

		internal static void WaitForSecond()
		{
			Thread.Sleep(TimeSpan.FromSeconds(1));
		}

		private static char GetDisplayOptionFromIndex(int i)
		{
			if (i < MAX_OPTION_N) {
				return Char.Parse(i.ToString());
			}

			int d = OPTION_LETTER_START + (i - MAX_OPTION_N);

			return (char) d;
		}

		private static int GetIndexFromDisplayOption(char c)
		{
			if (Char.IsNumber(c)) {
				int idx = (int) Char.GetNumericValue(c);
				return idx;
			}

			if (Char.IsLetter(c)) {
				c = Char.ToUpper(c);
				int d = MAX_OPTION_N + (c - OPTION_LETTER_START);

				return d;
			}

			return INVALID;
		}

		private static string FormatOption(ConsoleOption option, int i)
		{
			var sb = new StringBuilder();
			char c = GetDisplayOptionFromIndex(i);

			//todo

			var name = option.Name;
			sb.AppendFormat("[{0}]: {1} ", c, name);


			if (option.Data != null) {
				sb.Append(option.Data);
			}

			if (!sb.ToString().EndsWith("\n")) {
				sb.AppendLine();
			}

			string s = CliOutput.FormatString(ConsoleIO.CLI_CHAR, sb.ToString());

			return s;
		}

		private const int MAX_OPTION_N = 10;
		private const char OPTION_LETTER_START = 'A';
		private const int INVALID = -1;


		/// <summary>
		/// Escape -> quit
		/// </summary>
		private const ConsoleKey ESC_EXIT = ConsoleKey.Escape;

		/// <summary>
		/// Alt modifier -> View extra info
		/// </summary>
		private const ConsoleModifiers ALT_EXTRA = ConsoleModifiers.Alt;


		// todo

		/// <summary>
		/// Signals to continue displaying current interface
		/// </summary>
		internal const int STATUS_OK = 0;

		/// <summary>
		/// Signals to reload interface
		/// </summary>
		internal const int STATUS_REFRESH = 1;

		/// <summary>
		/// Interface status
		/// </summary>
		internal static int Status;

		/// <summary>
		///     Handles user input and options
		/// </summary>
		/// <param name="options">Array of <see cref="ConsoleOption" /></param>
		/// <param name="selectMultiple">Whether to return selected options as a <see cref="HashSet{T}"/></param>
		internal static HashSet<object> HandleOptions(IEnumerable<ConsoleOption> options, bool selectMultiple = false)
		{
			var i = new ConsoleInterface(options, null, selectMultiple);

			return HandleOptions(i);
		}

		/// <summary>
		///     Handles user input and options
		/// </summary>
		/// <param name="io"><see cref="ConsoleInterface"/></param>
		internal static HashSet<object> HandleOptions(ConsoleInterface io)
		{
			// todo: very hacky

			var selectedOptions = new HashSet<object>();

			void DisplayInterface()
			{
				CliOutput.WithColor(ConsoleColor.DarkRed, () =>
				{
					//Console.WriteLine(inter?.Name);
					Console.WriteLine(RuntimeInfo.NAME_BANNER);
				});

				
				for (int i = 0; i < io.Length; i++) {
					var option = io[i];

					var s = FormatOption(option, i);

					CliOutput.WithColor(option.Color, () =>
					{
						Console.Write(s);
					});

				}

				Console.WriteLine();

				// Show options
				if (io.SelectMultiple) {
					string optionsStr = selectedOptions.QuickJoin();

					CliOutput.WithColor(ConsoleColor.Blue, () =>
					{
						Console.WriteLine(optionsStr);
					});
				}

				// Handle key reading

				// @formatter:off — disable formatter after this line

				string prompt = String.Format("Enter the option number to open or {0} to exit.\n", ESC_EXIT) +
								String.Format("Hold down {0} while entering the option number to show more info.\n", ALT_EXTRA) +
								String.Format("Options with expanded information are denoted with {0}.", ConsoleIO.ALT_DENOTE);

				CliOutput.WriteSuccess(prompt);

				// @formatter:on — enable formatter after this line
			}


			/*
			 * Handle input
			 */


			ConsoleKeyInfo cki;

			do {
				Console.Clear();


				DisplayInterface();

				while (!Console.KeyAvailable) {
					// Block until input is entered.

					//
					if (Interlocked.Exchange(ref Status, STATUS_OK) == STATUS_REFRESH) {
						Console.Clear();
						DisplayInterface();
					}
				}


				// Key was read
				
				cki = Console.ReadKey(true);
				char keyChar = cki.KeyChar;
				var modifiers = cki.Modifiers;
				bool altModifier = (modifiers & ALT_EXTRA) != 0;

				// Handle option

				int idx = GetIndexFromDisplayOption(keyChar);

				if (idx < io.Length && idx >= 0) {

					var option = io[idx];

					bool useAltFunc = altModifier && option.AltFunction != null;

					if (useAltFunc) {

						var altFunc = option.AltFunction()!;

						//
					}
					else {
						var funcResult = option.Function()!;

						if (funcResult != null) {
							//
							if (io.SelectMultiple) {
								selectedOptions.Add(funcResult);
							}
							else {
								return new HashSet<object> {funcResult};
							}
						}
					}


				}


			} while (cki.Key != ESC_EXIT);

			return selectedOptions;
		}
	}
}