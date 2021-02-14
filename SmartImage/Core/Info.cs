#nullable enable
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using Novus;
using Novus.Runtime;
using Novus.Win32;
using SimpleCore.Cli;
using SimpleCore.Utilities;
using SmartImage.Configuration;
using SmartImage.Utilities;
using static SmartImage.Core.Interface;

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


		public static bool IsAppFolderInPath => FileSystem.IsFolderInPath(AppFolder);

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

			NConsole.Resize(ResultsWindowWidth, 30);

			var sb = new StringBuilder();
			sb.AppendColor(ColorPrimary, NAME_BANNER);
			sb.AppendLine();
			

			/*
			 * Author info
			 */



			sb.AppendLabelWithColor(ColorPrimary, "Author", ColorMisc2, Author).AppendLine();
			sb.AppendLabelWithColor(ColorPrimary, "Repo", ColorMisc2, Repo).AppendLine();
			sb.AppendLabelWithColor(ColorPrimary, "Readme", ColorMisc2, Readme).AppendLine();

			sb.AppendLine();

			/*
			 * Config
			 */

			sb.Append(SearchConfig.Config);



			/*
			 * Version info
			 */

			sb.AppendLine();

			var versionsInfo = UpdateInfo.GetUpdateInfo();

			sb.AppendLabelWithColor(ColorVersion,"Current version", ColorMisc2, versionsInfo.Current).AppendLine();
			sb.AppendLabelWithColor(ColorVersion,"Latest version", ColorMisc2, versionsInfo.Latest.Version).AppendLine();
			sb.AppendLabelWithColor(ColorVersion,"Version status", ColorMisc2, versionsInfo.Status).AppendLine();
			

			/*
			 * Runtime info
			 */

			sb.AppendLine();

			string appFolderName    = new DirectoryInfo(AppFolder).Name;
			var    exeFolderName = new DirectoryInfo(ExeLocation).Name;

			sb.AppendLabelWithColor(ColorUtility,"Application folder", ColorMisc2, appFolderName).AppendLine();
			sb.AppendLabelWithColor(ColorUtility,"Executable location", ColorMisc2, exeFolderName).AppendLine();
			sb.AppendLabelWithColor(ColorUtility,"Context menu integrated", ColorMisc2, Integration.IsContextMenuAdded).AppendLine();
			sb.AppendLabelWithColor(ColorUtility, "In path", ColorMisc2, IsAppFolderInPath).AppendLine();

			
			

			/*
			 * Dependencies
			 */

			// sb.AppendLine("Dependencies:");
			//
			// var dependencies = RuntimeInfo.DumpDependencies();
			//
			// foreach (var name in dependencies) {
			// 	sb.AppendColor(ColorMisc,$"{name.Name!} ({name.Version!})").AppendLine();
			// }

			NConsole.Write(sb);
		}
	}
}