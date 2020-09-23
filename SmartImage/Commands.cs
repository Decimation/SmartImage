using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using HtmlAgilityPack;
using Microsoft.Win32;
using RestSharp;
using SimpleCore.Utilities;
using SmartImage.Searching;
using SmartImage.Utilities;

// ReSharper disable UseStringInterpolation

// ReSharper disable ParameterTypeCanBeEnumerable.Global

namespace SmartImage
{
	internal enum IntegrationOption
	{
		Add,
		Remove
	}

	/// <summary>
	/// Program functionality and integration
	/// </summary>
	internal static class Commands
	{
		private const char CLI_CHAR = '*';

		internal static void RunContextMenuIntegration(IntegrationOption option)
		{
			// TODO: use default Registry library

			switch (option) {
				case IntegrationOption.Add:
					string fullPath = RuntimeInfo.ExeLocation;

					if (!RuntimeInfo.IsExeInAppFolder) {
						bool v = CliOutput.ReadConfirm("Could not find exe in system path. Add now?");

						if (v) {
							RuntimeInfo.Setup();
							return;
						}
					}

					// // Add command and icon to command
					// string[] commandCode =
					// {
					// 	"@echo off",
					// 	$"reg.exe add {RuntimeInfo.REG_SHELL_CMD} /ve /d \"{fullPath} \"\"%%1\"\"\" /f >nul",
					// 	$"reg.exe add {RuntimeInfo.REG_SHELL} /v Icon /d \"{fullPath}\" /f >nul"
					// };
					//
					// Cli.CreateRunBatchFile("add_to_menu.bat", commandCode);

					var cmd = RuntimeInfo.Subkey.CreateSubKey("command");
					cmd.SetValue(null, string.Format("\"{0}\" \"%1\"", fullPath));

					break;
				case IntegrationOption.Remove:
					// // reg delete HKEY_CLASSES_ROOT\*\shell\SmartImage
					//
					// // const string DEL = @"reg delete HKEY_CLASSES_ROOT\*\shell\SmartImage";
					//
					// string[] code =
					// {
					// 	"@echo off",
					// 	$@"reg.exe delete {RuntimeInfo.REG_SHELL} /f >nul"
					// };
					//
					// Cli.CreateRunBatchFile("rem_from_menu.bat", code);

					Registry.CurrentUser.DeleteSubKeyTree(RuntimeInfo.shell3);

					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(option), option, null);
			}
		}

		internal static void RunPathIntegration(IntegrationOption option)
		{
			switch (option) {
				case IntegrationOption.Add:
				{
					string oldValue = ExplorerSystem.EnvironmentPath;

					string appFolder = RuntimeInfo.AppFolder;

					if (RuntimeInfo.IsAppFolderInPath) return;


					bool appFolderInPath = oldValue.Split(ExplorerSystem.PATH_DELIM).Any(p => p == appFolder);

					string cd = Environment.CurrentDirectory;
					string exe = Path.Combine(cd, RuntimeInfo.NAME_EXE);

					if (!appFolderInPath) {
						string newValue = oldValue + ExplorerSystem.PATH_DELIM + cd;
						ExplorerSystem.EnvironmentPath = newValue;
					}

					break;
				}
				case IntegrationOption.Remove:
					ExplorerSystem.RemoveFromPath(RuntimeInfo.AppFolder);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(option), option, null);
			}
		}

		internal static void RunReset()
		{

			SearchConfig.Config.Reset();
			SearchConfig.Config.WriteToFile();

			// Computer\HKEY_CLASSES_ROOT\*\shell\SmartImage

			RunContextMenuIntegration(IntegrationOption.Remove);

			// will be added automatically if run again
			//Path.Remove();

			CliOutput.WriteSuccess("Reset cfg");
		}

		internal static void ShowInfo()
		{
			// todo

			Console.Clear();

			/*
			 * Config
			 */

			CliOutput.WriteInfo(SearchConfig.Config.Dump());


			/*
			 * Runtime info
			 */


			CliOutput.WriteInfo("Application folder: {0}", RuntimeInfo.AppFolder);
			CliOutput.WriteInfo("Executable location: {0}", RuntimeInfo.ExeLocation);
			CliOutput.WriteInfo("Context menu integrated: {0}", RuntimeInfo.IsContextMenuAdded);
			CliOutput.WriteInfo("In path: {0}\n", RuntimeInfo.IsAppFolderInPath);


			/*
			 * Version info
			 */

			var versionsInfo = UpdateInfo.CheckForUpdates();

			CliOutput.WriteInfo("Current version: {0}", versionsInfo.Current);
			CliOutput.WriteInfo("Latest version: {0}", versionsInfo.Latest.Version);
			CliOutput.WriteInfo("Version status: {0}", versionsInfo.Status);

			Console.WriteLine();

			/*
			 * Author info
			 */

			CliOutput.WriteInfo("Repo: {0}", RuntimeInfo.Repo);
			CliOutput.WriteInfo("Readme: {0}", RuntimeInfo.Readme);
			CliOutput.WriteInfo("Author: {0}", RuntimeInfo.Author);
		}


		/// <summary>
		///     Handles user input and options
		/// </summary>
		/// <param name="options">Array of <see cref="ConsoleOption" /></param>
		/// <param name="multiple">Whether to return selected options as a <see cref="HashSet{T}"/></param>
		internal static HashSet<object> HandleConsoleOptions(ConsoleOption[] options, bool multiple = false)
		{

			var selectedOptions = new HashSet<object>();

			const int MAX_OPTION_N = 10;
			const char OPTION_LETTER_START = 'A';
			const int INVALID = -1;

			static char ToDisplay(int i)
			{
				if (i < MAX_OPTION_N) {
					return Char.Parse(i.ToString());
				}

				int d = OPTION_LETTER_START + (i - MAX_OPTION_N);

				return (char) d;
			}

			static int FromDisplay(char c)
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

			const ConsoleKey ESC_EXIT = ConsoleKey.Escape;

			ConsoleKeyInfo cki;

			do {
				Console.Clear();

				CliOutput.WithColor(ConsoleColor.DarkRed, () =>
				{
					Console.WriteLine(RuntimeInfo.NAME_BANNER);
				});


				for (int i = 0; i < options.Length; i++) {
					var r = options[i];
					var sb = new StringBuilder();
					char c = ToDisplay(i);
					sb.AppendFormat("[{0}]: {1} ", c, r.Name);


					if (r.ExtendedName != null) {
						sb.Append(r.ExtendedName);
					}

					if (!sb.ToString().EndsWith("\n")) {
						sb.AppendLine();
					}

					string s = CliOutput.Prepare(CLI_CHAR, sb.ToString());

					CliOutput.WithColor(r.Color, () =>
					{
						Console.Write(s);
					});


				}

				Console.WriteLine();

				// Show options
				if (multiple) {
					string optionsStr = CommonUtilities.Join(selectedOptions);


					CliOutput.WithColor(ConsoleColor.Blue, () =>
					{
						Console.WriteLine(optionsStr);
					});
				}

				// Handle key reading

				CliOutput.WriteSuccess("Enter the result number to open or {0} to exit.", ESC_EXIT);


				while (!Console.KeyAvailable) {
					// Block until input is entered.
				}


				// Key was read

				cki = Console.ReadKey(true);
				char keyChar = cki.KeyChar;

				int idx = FromDisplay(keyChar);

				if (idx < options.Length && idx >= 0) {
					var option = options[idx];
					var funcResult = option.Function()!;

					if (funcResult != null) {
						//
						if (multiple) {

							selectedOptions.Add(funcResult);
						}
						else {
							return new HashSet<object> {funcResult};
						}
					}
				}


			} while (cki.Key != ESC_EXIT);

			return selectedOptions;
		}

		private static void Uninstall()
		{
			// autonomous uninstall routine

			// self destruct

			// todo: optimize this

			string batchCommands = String.Empty;
			string exeFileName = RuntimeInfo.ExeLocation;
			const string DEL_BAT_NAME = "SmartImage_Delete.bat";


			batchCommands += "@echo off\n";

			/* Wait approximately 4 seconds (so that the process is already terminated) */
			batchCommands += "ping 127.0.0.1 > nul\n";

			/* Delete executable */
			batchCommands += "echo y | del /F ";

			batchCommands += exeFileName + "\n";

			/* Delete this bat file */
			batchCommands += "echo y | del " + DEL_BAT_NAME;

			var dir = Path.Combine(Path.GetTempPath(), DEL_BAT_NAME);

			File.WriteAllText(dir, batchCommands);

			Process.Start(dir);
		}

		internal static void WaitForInput()
		{
			Console.WriteLine();
			Console.WriteLine("Press any key to continue...");
			Console.ReadLine();
		}

		internal static void WaitForTime()
		{
			Thread.Sleep(TimeSpan.FromSeconds(1));
		}

		/// <summary>
		///     Runs when no arguments are given (and when the executable is double-clicked)
		/// </summary>
		/// <remarks>
		///     More user-friendly menu
		/// </remarks>
		internal static void RunCommandMenu()
		{
			var options = new[]
			{
				// Main option
				new ConsoleOption(">>> Select image <<<", ConsoleColor.Yellow, () =>
				{
					Console.WriteLine("Drag and drop the image here.");
					Console.Write("Image: ");

					string img = Console.ReadLine();
					img = CommonUtilities.CleanString(img);

					SearchConfig.Config.Image = img;

					return true;
				}),

				new ConsoleOption("Show info", () =>
				{
					ShowInfo();

					WaitForInput();
					return null;
				}),
				new ConsoleOption("Reset all configuration", () =>
				{
					RunReset();

					WaitForTime();
					return null;
				}),
				new ConsoleOption("Add/remove context menu integration", () =>
				{
					bool ctx = RuntimeInfo.IsContextMenuAdded;

					if (!ctx) {
						RunContextMenuIntegration(IntegrationOption.Add);
						CliOutput.WriteSuccess("Added to context menu");
					}
					else {
						RunContextMenuIntegration(IntegrationOption.Remove);
						CliOutput.WriteSuccess("Removed from context menu");
					}

					WaitForTime();
					return null;
				}),
				new ConsoleOption("Configure search engines", () =>
				{
					var rgEnum = ConsoleOption.CreateOptionsFromEnum<SearchEngines>();
					var values = HandleConsoleOptions(rgEnum, true);

					var newValues = CommonUtilities.ReadEnumFromSet<SearchEngines>(values);

					CliOutput.WriteInfo(newValues);

					SearchConfig.Config.SearchEngines = newValues;

					WaitForInput();

					return null;
				}),
				new ConsoleOption("Configure priority engines", () =>
				{
					var rgEnum = ConsoleOption.CreateOptionsFromEnum<SearchEngines>();
					var values = HandleConsoleOptions(rgEnum, true);

					var newValues = CommonUtilities.ReadEnumFromSet<SearchEngines>(values);

					CliOutput.WriteInfo(newValues);

					SearchConfig.Config.PriorityEngines = newValues;

					WaitForTime();

					return null;
				}),
				new ConsoleOption("Configure SauceNao API key", () =>
				{
					Console.Write("API key: ");

					string sauceNaoAuth = Console.ReadLine();


					SearchConfig.Config.SauceNaoAuth = sauceNaoAuth;

					WaitForTime();
					return null;
				}),
				new ConsoleOption("Configure Imgur API key", () =>
				{
					Console.Write("API key: ");

					string imgurAuth = Console.ReadLine();

					SearchConfig.Config.ImgurAuth = imgurAuth;

					WaitForTime();
					return null;
				}),
				new ConsoleOption("Update config file", () =>
				{
					SearchConfig.Config.WriteToFile();

					WaitForTime();
					return null;
				}),
				new ConsoleOption("Check for updates", () =>
				{
					// TODO: WIP

					var v = UpdateInfo.CheckForUpdates();

					if ((v.Status == VersionStatus.Available)) {
						NetworkUtilities.OpenUrl(v.Latest.AssetUrl);
					}

					WaitForTime();
					return null;
				}),
				new ConsoleOption("Uninstall", () =>
				{
					RunReset();
					RunContextMenuIntegration(IntegrationOption.Remove);
					RunPathIntegration(IntegrationOption.Remove);

					File.Delete(SearchConfig.ConfigLocation);
					Uninstall();

					// No return

					Environment.Exit(0);

					return null;
				}),

#if DEBUG


				new ConsoleOption("Run test", () =>
				{
					var cd = new DirectoryInfo(Environment.CurrentDirectory);
					var cd2 = cd.Parent.Parent.Parent.Parent.ToString();

					var testImages = new[]
					{
						// "Test1.jpg", 
						"Test2.jpg"
					};
					var testImg = CommonUtilities.GetRandomElement(testImages);
					var img = Path.Combine(cd2,testImg );

					SearchConfig.Config.Image = img;
					SearchConfig.Config.PriorityEngines = SearchEngines.None;
					return true;
				}),
#endif

			};

			HandleConsoleOptions(options);
		}
	}
}