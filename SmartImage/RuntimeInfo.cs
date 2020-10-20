#region

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using SimpleCore.CommandLine;
using SimpleCore.Win32;
using SmartImage.Utilities;
// ReSharper disable UnusedMember.Global

// ReSharper disable UseStringInterpolation

// ReSharper disable MemberCanBePrivate.Global

#endregion

#pragma warning disable HAA0101, HAA0502, HAA0601

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

		/*
		 * HKEY_CLASSES_ROOT is an alias, a merging, of two other locations:
		 *		HKEY_CURRENT_USER\Software\Classes
		 *		HKEY_LOCAL_MACHINE\Software\Classes
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


		public static bool IsAppFolderInPath => Native.IsFolderInPath(AppFolder);


		private static string FindExecutableLocation(string exe)
		{

			// https://stackoverflow.com/questions/6041332/best-way-to-get-application-folder-path
			// var exeLocation1 = Assembly.GetEntryAssembly().Location;
			// var exeLocation2 = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
			// var exeLocation3 = System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase;
			// var exeLocation = AppDomain.CurrentDomain.BaseDirectory;

			//

			var rg = new List<string>
			{
				/* Current directory */
				Environment.CurrentDirectory,


				/* Executing directory */
				Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase!
					.Replace("file:///", String.Empty)
					.Replace("/", "\\"))!,

				
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

			Console.Clear();

			/*
			 * Config
			 */

			NConsole.WriteInfo(SearchConfig.Config);


			/*
			 * Runtime info
			 */


			NConsole.WriteInfo("Application folder: {0}", AppFolder);
			NConsole.WriteInfo("Executable location: {0}", ExeLocation);
			NConsole.WriteInfo("Context menu integrated: {0}", Integration.IsContextMenuAdded);
			NConsole.WriteInfo("In path: {0}\n", IsAppFolderInPath);


			/*
			 * Version info
			 */

			var versionsInfo = UpdateInfo.CheckForUpdates();

			NConsole.WriteInfo("Current version: {0}", versionsInfo.Current);
			NConsole.WriteInfo("Latest version: {0}", versionsInfo.Latest.Version);
			NConsole.WriteInfo("Version status: {0}", versionsInfo.Status);

			Console.WriteLine();

			/*
			 * Author info
			 */

			NConsole.WriteInfo("Repo: {0}", Repo);
			NConsole.WriteInfo("Readme: {0}", Readme);
			NConsole.WriteInfo("Author: {0}", Author);
		}
	}
}