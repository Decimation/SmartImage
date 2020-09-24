#region

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Win32;
using SimpleCore;
using Newtonsoft.Json.Linq;
using RestSharp;
using SimpleCore.Utilities;
using SimpleCore.Win32;
using SimpleCore.Win32.Cli;
using SmartImage.Searching;
using SmartImage.Shell;
using SmartImage.Utilities;

// ReSharper disable UseStringInterpolation

// ReSharper disable MemberCanBePrivate.Global

#endregion


namespace SmartImage
{
	/// <summary>
	/// Program runtime information and config
	/// </summary>
	public static class RuntimeInfo
	{
		/// <summary>
		/// Name in ASCII art
		/// </summary>
		public const string NAME_BANNER =
			"  ____                       _   ___\n" +
			" / ___| _ __ ___   __ _ _ __| |_|_ _|_ __ ___   __ _  __ _  ___\n" +
			@" \___ \| '_ ` _ \ / _` | '__| __|| || '_ ` _ \ / _` |/ _` |/ _ \" + "\n" +
			"  ___) | | | | | | (_| | |  | |_ | || | | | | | (_| | (_| |  __/\n" +
			@" |____/|_| |_| |_|\__,_|_|   \__|___|_| |_| |_|\__,_|\__, |\___|" + "\n" +
			"                                                     |___/\n";

		public const string NAME = "SmartImage";

		public const string NAME_EXE = "SmartImage.exe";

		public const string NAME_CFG = "SmartImage.cfg";

		public const string Author = "Read Stanton";

		public const string Repo = "https://github.com/Decimation/SmartImage";

		public const string Readme = "https://github.com/Decimation/SmartImage/blob/master/README.md";

		public const string Issue = "https://github.com/Decimation/SmartImage/issues/new";

		public const string REG_SHELL = @"HKEY_CLASSES_ROOT\*\shell\SmartImage\";
		
		public const string REG_SHELL_CMD = @"HKEY_CLASSES_ROOT\*\shell\SmartImage\command";

		//public const string REG_SHELL = @"HKEY_CURRENT_USER\Software\Classes\*\shell\SmartImage\";

		//public const string REG_SHELL_CMD = @"HKEY_CURRENT_USER\Software\Classes\*\shell\SmartImage\command";
		
		// todo

		internal const string shell3 = @"Software\Classes\*\shell\SmartImage\";

		/*
		 * HKEY_CLASSES_ROOT is an alias, a merging, of two other locations:
		 *		HKEY_CURRENT_USER\Software\Classes
		 *		HKEY_LOCAL_MACHINE\Software\Classes
		 */

		/*
		 * todo: !!! switch from using batch files to interact with registry to the Registry library !!!
		 */

		public static string AppFolder
		{

			// todo: use ProgramData

			get
			{
				string? folder = Path.GetDirectoryName(ExeLocation);
				Debug.Assert(folder != null);
				return folder;
			}
		}

		public static bool IsExeInAppFolder => File.Exists(Path.Combine(AppFolder, NAME_EXE));

		/// <summary>
		///     Null if executable is not in path.
		/// </summary>
		public static string ExeLocation => FindExecutableLocation(NAME_EXE);


		// todo
		internal static RegistryKey Subkey => Registry.CurrentUser.CreateSubKey(shell3);

		public static bool IsContextMenuAdded
		{
			get
			{
				// TODO: use default Registry library

				var shell=Registry.CurrentUser.OpenSubKey(@"Software\Classes\*\shell\");
				return shell.GetSubKeyNames().Contains(NAME);

				//return Subkey.GetSubKeyNames().Contains("command");

				/*string cmdStr = String.Format(@"reg query {0}", REG_SHELL_CMD);
				var cmd = Cli.Shell(cmdStr, true);

				string[] stdOut = Cli.ReadAllLines(cmd.StandardOutput);

				// todo
				if (stdOut.Any(s => s.Contains(NAME))) {
					return true;
				}

				string[] stdErr = Cli.ReadAllLines(cmd.StandardError);

				if (stdErr.Any(s => s.Contains("ERROR"))) {
					return false;
				}


				//


				throw new SmartImageException();*/
			}
		}

		public static void Setup()
		{
			if (!IsAppFolderInPath) {
				Integration.HandlePath(IntegrationOption.Add);
			}
		}

		public static bool IsAppFolderInPath => Native.IsFolderInPath(AppFolder);


		private static string FindExecutableLocation(string exe)
		{

			// https://stackoverflow.com/questions/6041332/best-way-to-get-application-folder-path
			// var exeLocation1 = Assembly.GetEntryAssembly().Location;
			// var exeLocation2 = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
			// var exeLocation3 = System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase;
			// var exeLocation = AppDomain.CurrentDomain.BaseDirectory;

			//

			var rg = new List<string>()
			{
				/* Executing directory */
				Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase!
					.Replace("file:///", String.Empty)
					.Replace("/", "\\"))!,

				/* Current directory */
				Environment.CurrentDirectory
			};

			rg.AddRange(Native.PathDirectories);

			//

			foreach (string loc in rg) {
				if (ExistsInFolder(loc, exe, out var folder)) {
					return folder;
				}
			}


			static bool ExistsInFolder(string folder, string exeStr, out string folderExe)
			{
				string folderExeFull = Path.Combine(folder, exeStr);
				bool inFolder = File.Exists(folderExeFull);

				folderExe = folderExeFull;
				return inFolder;
			}

			throw new SmartImageException();
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
	}
}