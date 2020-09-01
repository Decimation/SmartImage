using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using SimpleCore.Utilities;
using SmartImage.Model;

// ReSharper disable UseStringInterpolation

// ReSharper disable ParameterTypeCanBeEnumerable.Global

namespace SmartImage
{


	public static class Commands
	{
		public const string OPT_ADD = "add";
		public const string OPT_REM = "remove";
		public const string OPT_ALL = "all";

		public static void RunContextMenuIntegration(string option)
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

		public static void RunPathIntegration(string option)
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

		public static void RunReset(string option)
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

		public static void ShowInfo()
		{
			Console.Clear();

			CliOutput.WriteInfo("Search engines: {0}", SearchConfig.Config.Engines);
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


			//

			// CliOutput.WriteInfo("Supported search engines: {0}\n", SearchEngines.All);

			//

			var asm = typeof(RuntimeInfo).Assembly.GetName();
			var currentVersion = asm.Version;
			CliOutput.WriteInfo("Current version: {0}", currentVersion);

			/*var release = ReleaseInfo.LatestRelease();
			CliOutput.WriteInfo("Latest version: {0} (tag {1}) ({2})", release.Version, release.TagName,
								release.PublishedAt);

			int vcmp = currentVersion.CompareTo(release.Version);

			if (vcmp < 0) {
				CliOutput.WriteInfo("Update available");
			}
			else if (vcmp == 0) {
				CliOutput.WriteInfo("Up to date");
			}
			else {
				CliOutput.WriteInfo("(preview)");
			}*/

			CliOutput.WriteInfo("Readme: {0}", RuntimeInfo.Readme);
			CliOutput.WriteInfo("Author: {0}", RuntimeInfo.Author);
		}

		/// <summary>
		/// Handles user input 
		/// </summary>
		/// <param name="rg"><see cref="ConsoleOption"/></param>
		public static void HandleConsoleOptions(ConsoleOption[] rg)
		{
			ConsoleKeyInfo cki;

			do {
				Console.Clear();
				Console.WriteLine(RuntimeInfo.NAME_BANNER);

				for (int i = 0; i < rg.Length; i++) {
					var r = rg[i];
					var sb = new StringBuilder();
					sb.AppendFormat("[{0}]: {1} ", i, r.Name);


					if (r.ExtendedName!=null) {
						sb.Append(r.ExtendedName);
					}

					if (!sb.ToString().EndsWith("\n")) {
						sb.AppendLine();
					}

					Console.Write(sb);
				}

				Console.WriteLine();

				CliOutput.WriteSuccess("Enter the result number to open or escape to quit.");


				while (!Console.KeyAvailable) {
					// Block until input is entered.
				}


				// Key was read

				cki = Console.ReadKey(true);
				char keyChar = cki.KeyChar;

				if (Char.IsNumber(keyChar)) {
					int idx = (int) Char.GetNumericValue(cki.KeyChar);

					if (idx < rg.Length && idx >= 0) {
						var option = rg[idx];
						var funcResult = option.Function()!;

						if (funcResult != null) {
							//
							return;
						}
					}
				}
			} while (cki.Key != ConsoleKey.Escape);
		}

		public static void Pause()
		{
			Console.WriteLine();
			Console.WriteLine("Press any key to continue...");
			Console.ReadLine();
		}

		public static void Wait()
		{
			Thread.Sleep(TimeSpan.FromSeconds(1));
		}

		/// <summary>
		/// Runs when no arguments are given (and when the executable is double-clicked)
		/// 
		/// </summary>
		/// <remarks>
		/// More user-friendly menu
		/// </remarks>
		public static void RunCommandMenu()
		{
			var options = new[]
			{
				new ConsoleOption("* Select image", () =>
				{
					Console.WriteLine("Drag and drop the image here.");
					Console.Write("Image: ");

					var img = Console.ReadLine();
					img = Utilities.CleanString(img);

					SearchConfig.Config.Image = img;

					return true;
				}),
				new ConsoleOption("Show info", () =>
				{
					ShowInfo();

					Pause();
					return null;
				}),
				new ConsoleOption("Reset all", () =>
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
			};

			HandleConsoleOptions(options);
		}
	}
}