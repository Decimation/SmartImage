#nullable enable
using JetBrains.Annotations;
using Novus.Win32;
using SmartImage.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Kantan.Cli;
using Kantan.Diagnostics;
// ReSharper disable CognitiveComplexity

// ReSharper disable PossibleNullReferenceException

// ReSharper disable UnusedMember.Global

// ReSharper disable UseStringInterpolation

// ReSharper disable MemberCanBePrivate.Global

#nullable disable
namespace SmartImage.Core
{
	/// <summary>
	/// Program runtime information
	/// </summary>
	public static class AppInfo
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

		public const string Wiki = "https://github.com/Decimation/SmartImage/wiki";

		public static string AppFolder => Path.GetDirectoryName(ExeLocation);

		public static Version Version => typeof(AppInfo).Assembly.GetName().Version!;

		public static bool IsExeInAppFolder => File.Exists(Path.Combine(AppFolder, NAME_EXE));

		/// <summary>
		///     <c>Null</c> if executable is not in path.
		/// </summary>
		public static string ExeLocation
		{
			get
			{
				var module = Process.GetCurrentProcess().MainModule;
				Guard.AssertNotNull(module);

				return module.FileName;
			}
		}

		


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

			if (!IsAppFolderInPath) {
				AppIntegration.HandlePath(IntegrationOption.Add);
			}

			var languages = Windows.System.UserProfile.GlobalizationPreferences.Languages;

			bool zh = languages.Any(l => l.Contains("zh"));

			bool ja = languages.Contains("ja");

			if (ja || zh) {

				Console.WriteLine("Non-Romance language detected!");
				Console.WriteLine("If English is not the main IME, things may not work properly!");

				NConsole.WaitForInput();
			}

			//Windows.System.UserProfile.GlobalizationPreferences.Languages
			//Thread.CurrentThread.CurrentUICulture
			//CultureInfo.CurrentCulture
		}
	}
}