#nullable enable
using JetBrains.Annotations;
using Novus.Win32;
using SimpleCore.Cli;
using SimpleCore.Utilities;
using SmartImage.Utilities;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

// ReSharper disable UnusedMember.Global

// ReSharper disable UseStringInterpolation

// ReSharper disable MemberCanBePrivate.Global


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
			"  ____                       _   ___\n"                             +
			" / ___| _ __ ___   __ _ _ __| |_|_ _|_ __ ___   __ _  __ _  ___\n"  +
			@" \___ \| '_ ` _ \ / _` | '__| __|| || '_ ` _ \ / _` |/ _` |/ _ \"  + "\n" +
			"  ___) | | | | | | (_| | |  | |_ | || | | | | | (_| | (_| |  __/\n" +
			@" |____/|_| |_| |_|\__,_|_|   \__|___|_| |_| |_|\__,_|\__, |\___|"  + "\n" +
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
		/// Config file name
		/// </summary>
		public const string NAME_CFG = "SmartImage.cfg";

		public const string Author = "Read Stanton";

		public const string Repo = "https://github.com/Decimation/SmartImage";

		public const string Readme = "https://github.com/Decimation/SmartImage/blob/master/README.md";

		public const string Issue = "https://github.com/Decimation/SmartImage/issues/new";


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

		public static Version Version => typeof(Info).Assembly.GetName().Version!;

		public static bool IsExeInAppFolder => File.Exists(Path.Combine(AppFolder, NAME_EXE));

		/// <summary>
		///     <c>Null</c> if executable is not in path.
		/// </summary>
		[MaybeNull]
		public static string ExeLocation => FileSystem.FindExecutableLocation(NAME_EXE)!;


		public static bool IsAppFolderInPath => FileSystem.IsFolderInPath(AppFolder);

		/// <summary>
		/// Setup
		/// </summary>
		[ModuleInitializer]
		public static void Setup()
		{
			if (!OperatingSystem.IsWindows()) {
				throw new NotSupportedException();
			}

			OSIntegration.Setup();
		}
	}
}