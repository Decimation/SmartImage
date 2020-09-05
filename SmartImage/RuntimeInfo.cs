#region

#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using SimpleCore;
using Newtonsoft.Json.Linq;
using RestSharp;
using SimpleCore.Utilities;
using SmartImage.Searching;

// ReSharper disable UseStringInterpolation

// ReSharper disable MemberCanBePrivate.Global

#endregion


namespace SmartImage
{
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

		public const string Readme = "https://github.com/Decimation/SmartImage/blob/master/README.md";

		public const string REG_SHELL = @"HKEY_CLASSES_ROOT\*\shell\SmartImage\";

		public const string REG_SHELL_CMD = @"HKEY_CLASSES_ROOT\*\shell\SmartImage\command";

		public static string AppFolder
		{
			get
			{
				string folder = Path.GetDirectoryName(ExeLocation);
				Debug.Assert(folder != null);
				return folder;
			}
		}

		public static string ConfigLocation => Path.Combine(AppFolder, NAME_CFG);

		public static bool IsExeInAppFolder => File.Exists(Path.Combine(AppFolder, NAME_EXE));

		/// <summary>
		///     Null if executable is not in path.
		/// </summary>
		public static string ExeLocation => FindExecutableLocation(NAME_EXE);

		public static bool IsContextMenuAdded
		{
			get
			{
				string cmdStr = String.Format(@"reg query {0}", REG_SHELL_CMD);
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


				throw new InvalidOperationException();
			}
		}

		public static void Setup()
		{
			if (!IsAppFolderInPath) {
				Commands.RunPathIntegration(Commands.OPT_ADD);
			}
		}

		public static bool IsAppFolderInPath => ExplorerSystem.IsFolderInPath(AppFolder);


		private static string FindExecutableLocation(string exe)
		{
			string path = ExplorerSystem.FindExectableInPath(exe);

			if (path == null) {
				string cd = Environment.CurrentDirectory;

				if (Try(cd, exe, out path)) {
					return path;
				}


				// SPECIAL CASE: app folder is not in path, continuing past here causes a stack overflow
				// todo


				/*if (Try(AppFolder, exe, out path)) {
					return path;
				}*/
			}

			static bool Try(string folder, string exeStr, out string folderExe)
			{
				string folderExeFull = Path.Combine(folder, exeStr);
				bool inFolder = File.Exists(folderExeFull);

				folderExe = folderExeFull;
				return inFolder;
			}

			return path;
		}
	}
}