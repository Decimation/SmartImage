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
using Kantan.Text;
using Kantan.Utilities;
using Novus;
using SmartImage.Lib.Utilities;
using static Kantan.Diagnostics.LogCategories;

// ReSharper disable CognitiveComplexity

// ReSharper disable PossibleNullReferenceException

// ReSharper disable UnusedMember.Global

// ReSharper disable UseStringInterpolation

// ReSharper disable MemberCanBePrivate.Global

#nullable disable
namespace SmartImage.Core;

/// <summary>
/// Program runtime information
/// </summary>
public static class AppInfo
{
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
		/*if (!OperatingSystem.IsWindows()) {
			throw new NotSupportedException();
		}*/
			
		// Remove old path directories
		var pathDirectories = FileSystem.GetEnvironmentPathDirectories();
		var oldFolders      = pathDirectories.Where(x=>x.Contains(NAME) && x!= AppFolder);

		foreach (string s in oldFolders) {
			FileSystem.RemoveFromPath(s);
		}
		
		if (!IsAppFolderInPath) {
			AppIntegration.HandlePath(true);
		}

		Debug.WriteLine($"Cli utilities: {ImageHelper.Utilities.QuickJoin()}", C_INFO);
	}
}