#nullable enable
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using Novus;
using Novus.Runtime;
using Novus.Win32;
using SimpleCore.Cli;
using SmartImage.Utilities;

// ReSharper disable UnusedMember.Global

// ReSharper disable UseStringInterpolation

// ReSharper disable MemberCanBePrivate.Global

#pragma warning disable HAA0101, HAA0502, HAA0601, RCS1036

namespace SmartImage.Core
{
	/// <summary>
	/// Program runtime information
	/// </summary>
	public static class Info
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

		
		/// <summary>
		/// Name
		/// </summary>
		public const string NAME = "SmartImage";

		/// <summary>
		/// Executable file name
		/// </summary>
		public const string NAME_EXE = "SmartImage.exe";

		/// <summary>
		/// Config file name (<see cref="SearchConfig"/>)
		/// </summary>
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
			// todo: use ProgramData?

			get
			{
				string? folder = Path.GetDirectoryName(ExeLocation);
				Debug.Assert(folder != null);
				return folder;
			}
		}

		public static bool IsExeInAppFolder => File.Exists(Path.Combine(AppFolder, NAME_EXE));

		/// <summary>
		///     <c>Null</c> if executable is not in path.
		/// </summary>
		[CanBeNull]
		public static string ExeLocation => FileSystem.FindExecutableLocation(NAME_EXE)!;


		public static bool IsAppFolderInPath => OS.IsFolderInPath(AppFolder);

		/// <summary>
		/// Setup
		/// </summary>
		public static void Setup()
		{
			if (!OperatingSystem.IsWindows()) {
				throw new NotSupportedException();
			}
		}

		internal static void ShowInfo()
		{

			Console.Clear();

			Console.WriteLine(Info.NAME_BANNER);
			
			/*
			 * Author info
			 */

			NConsole.OverrideForegroundColor = Interface.ColorMain;

			NConsole.WriteInfo("Author: {0}", Author);
			NConsole.WriteInfo("Repo: {0}", Repo);
			NConsole.WriteInfo("Readme: {0}", Readme);

			NConsole.NewLine();

			NConsole.ResetOverrideColors();

			/*
			 * Config
			 */

			NConsole.OverrideForegroundColor = Interface.ColorConfig;

			NConsole.WriteInfo(SearchConfig.Config);

			NConsole.ResetOverrideColors();


			/*
			 * Version info
			 */

			NConsole.OverrideForegroundColor = Interface.ColorVersion;

			var versionsInfo = UpdateInfo.GetUpdateInfo();

			NConsole.WriteInfo("Current version: {0}", versionsInfo.Current);
			NConsole.WriteInfo("Latest version: {0}", versionsInfo.Latest.Version);
			NConsole.WriteInfo("Version status: {0}", versionsInfo.Status);

			NConsole.NewLine();

			NConsole.ResetOverrideColors();

			/*
			 * Runtime info
			 */

			NConsole.OverrideForegroundColor = Interface.ColorUtility;
			
			NConsole.WriteInfo("Application folder: {0}", AppFolder);
			NConsole.WriteInfo("Executable location: {0}", ExeLocation);
			NConsole.WriteInfo("Context menu integrated: {0}", Integration.IsContextMenuAdded);
			NConsole.WriteInfo("In path: {0}\n", IsAppFolderInPath);

			NConsole.ResetOverrideColors();

			

			/*
			 * Dependencies
			 */

			NConsole.WriteInfo("Dependencies:");

			var dependencies = RuntimeInfo.DumpDependencies();

			foreach (var name in dependencies) {
				NConsole.WriteInfo("{0} ({1})", name.Name!, name.Version!);
			}
		}
	}
}