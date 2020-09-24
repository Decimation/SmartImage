using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using SimpleCore.Win32;
using SimpleCore.Win32.Cli;
using SmartImage.Utilities;

// ReSharper disable UseStringInterpolation

// ReSharper disable ParameterTypeCanBeEnumerable.Global

namespace SmartImage.Shell
{
	

	/// <summary>
	/// Program functionality, IO, console functionality
	/// </summary>
	internal static class Commands
	{
		// todo: add ANSI sequence features like bold?

		private const char CLI_CHAR = '*';

		internal const string ALT_DENOTE = "[Alt]";


		/// <summary>
		///     Handles user input and options
		/// </summary>
		/// <param name="options">Array of <see cref="ConsoleOption" /></param>
		/// <param name="selectMultiple">Whether to return selected options as a <see cref="HashSet{T}"/></param>
		internal static HashSet<object> HandleConsoleOptions(ConsoleOption[] options, bool selectMultiple = false)
		{
			// TODO: create a way to handle nested options

			var selectedOptions = new HashSet<object>();

			const int MAX_OPTION_N = 10;
			const char OPTION_LETTER_START = 'A';
			const int INVALID = -1;

			static char ToDisplayOption(int i)
			{
				if (i < MAX_OPTION_N) {
					return Char.Parse(i.ToString());
				}

				int d = OPTION_LETTER_START + (i - MAX_OPTION_N);

				return (char) d;
			}

			static int FromDisplayOption(char c)
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


			/*
			 * Handle input
			 */

			// Escape -> quit
			const ConsoleKey ESC_EXIT = ConsoleKey.Escape;

			// Alt modifier -> View extra info
			const ConsoleModifiers ALT_EXTRA = ConsoleModifiers.Alt;

			ConsoleKeyInfo cki;

			do {
				Console.Clear();

				CliOutput.WithColor(ConsoleColor.DarkRed, () =>
				{
					Console.WriteLine(RuntimeInfo.NAME_BANNER);
				});


				for (int i = 0; i < options.Length; i++) {
					var option = options[i];
					var sb = new StringBuilder();
					char c = ToDisplayOption(i);


					var name = option.Name;
					sb.AppendFormat("[{0}]: {1} ", c, name);


					if (option.Data != null) {
						sb.Append(option.Data);
					}

					if (!sb.ToString().EndsWith("\n")) {
						sb.AppendLine();
					}

					string s = CliOutput.FormatString(CLI_CHAR, sb.ToString());

					CliOutput.WithColor(option.Color, () =>
					{
						Console.Write(s);
					});


				}

				Console.WriteLine();

				// Show options
				if (selectMultiple) {
					string optionsStr = selectedOptions.QuickJoin();


					CliOutput.WithColor(ConsoleColor.Blue, () =>
					{
						Console.WriteLine(optionsStr);
					});
				}

				// Handle key reading

// @formatter:off — disable formatter after this line

				string prompt = string.Format("Enter the result number to open or {0} to exit.\n", ESC_EXIT) +
				                string.Format("Hold down {0} while entering the result number to show more info.\n", ALT_EXTRA) +
				                string.Format("Results with expanded information are denoted with {0}.", Commands.ALT_DENOTE);

				CliOutput.WriteSuccess(prompt);

// @formatter:on — enable formatter after this line


				while (!Console.KeyAvailable) {
					// Block until input is entered.
				}


				// Key was read

				cki = Console.ReadKey(true);
				char keyChar = cki.KeyChar;
				var modifiers = cki.Modifiers;
				bool altModifier = (modifiers & ConsoleModifiers.Alt) != 0;

				int idx = FromDisplayOption(keyChar);

				if (idx < options.Length && idx >= 0) {
					var option = options[idx];
					bool useAltFunc = altModifier && option.AltFunction != null;

					if (useAltFunc) {

						var altFunc = option.AltFunction()!;

						//
					}
					else {
						var funcResult = option.Function()!;

						if (funcResult != null) {
							//
							if (selectMultiple) {

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

		/// <summary>
		///     Runs when no arguments are given (and when the executable is double-clicked)
		/// </summary>
		/// <remarks>
		///     More user-friendly menu
		/// </remarks>
		internal static void RunMainCommandMenu() => HandleConsoleOptions(RuntimeConsoleOptions.AllOptions);
	}
}