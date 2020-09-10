using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using RestSharp;
using SimpleCore.Utilities;
using SmartImage.Searching;
using SmartImage.Utilities;

// ReSharper disable UseStringInterpolation

// ReSharper disable ParameterTypeCanBeEnumerable.Global

namespace SmartImage
{
	internal static class Commands
	{
		internal const string OPT_ADD = "add";
		internal const string OPT_REM = "remove";
		internal const string OPT_ALL = "all";

		private const char CLI_CHAR = '*';

		internal static void RunContextMenuIntegration(string option)
		{
			switch (option) {
				case OPT_ADD:
					string fullPath = RuntimeInfo.ExeLocation;

					if (!RuntimeInfo.IsExeInAppFolder) {
						bool v = CliOutput.ReadConfirm("Could not find exe in system path. Add now?");

						if (v) {
							RuntimeInfo.Setup();
							return;
						}
					}

					// Add command
					string[] commandCode =
					{
						"@echo off",
						String.Format("reg.exe add {0} /ve /d \"{1} \"\"%%1\"\"\" /f >nul",
							RuntimeInfo.REG_SHELL_CMD, fullPath)
					};

					Cli.CreateRunBatchFile("add_to_menu.bat", commandCode);


					// Add icon
					string[] iconCode =
					{
						"@echo off",
						String.Format("reg.exe add {0} /v Icon /d \"{1}\" /f >nul", RuntimeInfo.REG_SHELL, fullPath)
					};

					Cli.CreateRunBatchFile("add_icon_to_menu.bat", iconCode);
					break;
				case OPT_REM:
					// reg delete HKEY_CLASSES_ROOT\*\shell\SmartImage

					// const string DEL = @"reg delete HKEY_CLASSES_ROOT\*\shell\SmartImage";

					string[] code =
					{
						"@echo off",
						String.Format(@"reg.exe delete {0} /f >nul", RuntimeInfo.REG_SHELL)
					};

					Cli.CreateRunBatchFile("rem_from_menu.bat", code);
					break;
			}
		}

		internal static void RunPathIntegration(string option)
		{
			switch (option) {
				case OPT_ADD:
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
				case OPT_REM:
					ExplorerSystem.RemoveFromPath(RuntimeInfo.AppFolder);
					break;
			}
		}

		internal static void RunReset(string option)
		{
			bool all = option == OPT_ALL;

			SearchConfig.Config.Reset();

			// Computer\HKEY_CLASSES_ROOT\*\shell\SmartImage

			RunContextMenuIntegration(OPT_REM);

			// will be added automatically if run again
			//Path.Remove();

			if (all) {
				SearchConfig.Config.Reset();
				SearchConfig.Config.WriteToFile();

				CliOutput.WriteSuccess("Reset cfg");
			}
		}

		internal static void ShowInfo()
		{
			Console.Clear();


			// Config

			CliOutput.WriteInfo("Search engines: {0}", SearchConfig.Config.SearchEngines);
			CliOutput.WriteInfo("Priority engines: {0}", SearchConfig.Config.PriorityEngines);

			string sn = SearchConfig.Config.SauceNaoAuth;
			bool snNull = String.IsNullOrWhiteSpace(sn);

			CliOutput.WriteInfo("SauceNao authentication: {0} ({1})",
				snNull ? CliOutput.MUL_SIGN.ToString() : sn, snNull ? "Basic" : "Advanced");

			string imgur = SearchConfig.Config.ImgurAuth;
			bool imgurNull = String.IsNullOrWhiteSpace(imgur);

			CliOutput.WriteInfo("Imgur authentication: {0}",
				imgurNull ? CliOutput.MUL_SIGN.ToString() : imgur);

			CliOutput.WriteInfo("Image upload service: {0}",
				imgurNull ? "ImgOps" : "Imgur");

			CliOutput.WriteInfo("Application folder: {0}", RuntimeInfo.AppFolder);
			CliOutput.WriteInfo("Executable location: {0}", RuntimeInfo.ExeLocation);
			
			CliOutput.WriteInfo("Config location: {0}", RuntimeInfo.ConfigLocation);
			CliOutput.WriteInfo("Context menu integrated: {0}", RuntimeInfo.IsContextMenuAdded);
			CliOutput.WriteInfo("In path: {0}\n", RuntimeInfo.IsAppFolderInPath);


			// Version

			var versionsInfo = VersionsInfo.Create();

			CliOutput.WriteInfo("Current version: {0}", versionsInfo.Current);
			CliOutput.WriteInfo("Latest version: {0}", versionsInfo.Latest.Version);
			CliOutput.WriteInfo("{0}", versionsInfo.Status);

			Console.WriteLine();

			// Author

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

				int d = OPTION_LETTER_START + (i-MAX_OPTION_N);
				
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
					string optionsStr = Common.Join(selectedOptions);


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

		internal static void SelfDestruct()
		{
			string batchCommands = string.Empty;
			string exeFileName = RuntimeInfo.ExeLocation;
			string batname = "SmartImage_Delete.bat";

			batchCommands += "@ECHO OFF\n";                         // Do not show any output
			batchCommands += "ping 127.0.0.1 > nul\n";              // Wait approximately 4 seconds (so that the process is already terminated)
			batchCommands += "echo y | del /F ";                    // Delete the executeable
			batchCommands += exeFileName + "\n";
			batchCommands += "echo y | del " + batname;    // Delete this bat file

			var dir = Path.Combine(Path.GetTempPath(), batname);

			File.WriteAllText(dir, batchCommands);

			Process.Start(dir);
		}

		internal static void Pause()
		{
			Console.WriteLine();
			Console.WriteLine("Press any key to continue...");
			Console.ReadLine();
		}

		internal static void Wait()
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
					img = Common.CleanString(img);

					SearchConfig.Config.Image = img;

					return true;
				}),

				new ConsoleOption("Show info", () =>
				{
					ShowInfo();

					Pause();
					return null;
				}),
				new ConsoleOption("Reset all configuration", () =>
				{
					RunReset(OPT_ALL);

					Wait();
					return null;
				}),
				new ConsoleOption("Add/remove context menu integration", () =>
				{
					bool ctx = RuntimeInfo.IsContextMenuAdded;

					if (!ctx) {
						RunContextMenuIntegration(OPT_ADD);
						CliOutput.WriteSuccess("Added to context menu");
					}
					else {
						RunContextMenuIntegration(OPT_REM);
						CliOutput.WriteSuccess("Removed from context menu");
					}

					Wait();
					return null;
				}),
				new ConsoleOption("Configure search engines", () =>
				{
					var rgEnum = ConsoleOption.CreateOptionsFromEnum<SearchEngines>();
					var values = HandleConsoleOptions(rgEnum, true);

					var newValues = Common.ReadEnumFromSet<SearchEngines>(values);

					CliOutput.WriteInfo(newValues);

					SearchConfig.Config.SearchEngines = newValues;

					Pause();

					return null;
				}),
				new ConsoleOption("Configure priority engines", () =>
				{
					var rgEnum = ConsoleOption.CreateOptionsFromEnum<SearchEngines>();
					var values = HandleConsoleOptions(rgEnum, true);

					var newValues = Common.ReadEnumFromSet<SearchEngines>(values);

					CliOutput.WriteInfo(newValues);

					SearchConfig.Config.PriorityEngines = newValues;

					Wait();

					return null;
				}),
				new ConsoleOption("Configure SauceNao API key", () =>
				{
					Console.Write("API key: ");

					string sauceNaoAuth = Console.ReadLine();


					SearchConfig.Config.SauceNaoAuth = sauceNaoAuth;

					Wait();
					return null;
				}),
				new ConsoleOption("Configure Imgur API key", () =>
				{
					Console.Write("API key: ");

					string imgurAuth = Console.ReadLine();

					SearchConfig.Config.ImgurAuth = imgurAuth;

					Wait();
					return null;
				}),
				new ConsoleOption("Update config file", () =>
				{
					SearchConfig.Config.WriteToFile();

					Wait();
					return null;
				}),
				new ConsoleOption("Check for updates", () =>
				{
					// TODO: WIP

					var v = VersionsInfo.Create();

					if ((v.Status == VersionStatus.Available)) {
						WebAgent.OpenUrl(v.Latest.AssetUrl);
					}

					Wait();
					return null;
				}),
				new ConsoleOption("Uninstall", () =>
				{
					RunReset(OPT_ALL);
					RunContextMenuIntegration(OPT_REM);
					RunPathIntegration(OPT_REM);

					File.Delete(RuntimeInfo.ConfigLocation);
					SelfDestruct();

					// No return

					Environment.Exit(0);

					return null;
				}),
				new ConsoleOption("Test", () =>
				{
					var rc = new RestClient("http://www.tineye.com/");
					
					rc.FollowRedirects = false;
					var re = new RestRequest("search", Method.POST);
					re.AddParameter("url","https://i.pximg.net/img-original/img/2019/10/23/08/48/13/77437257_p0.png", ParameterType.QueryString);
					
					var fu = rc.BuildUri(re);
					Console.WriteLine(fu.AbsoluteUri);
					var resp = rc.Execute(re);
					
					WebAgent.WriteResponse(resp);
					Console.WriteLine(resp.ResponseUri.AbsoluteUri);

					foreach (var respHeader in resp.Headers) {
						Console.WriteLine("{0} {1}",respHeader.Name, respHeader.Value);
					}
					Pause();

					return null;
				}),
			};

			HandleConsoleOptions(options);
		}
	}
}