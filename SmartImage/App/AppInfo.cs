#nullable enable
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Kantan.Diagnostics;
using Kantan.Text;
using Novus;
using Novus.OS;
using SmartImage.Lib.Utilities;

// ReSharper disable CognitiveComplexity

// ReSharper disable PossibleNullReferenceException

// ReSharper disable UnusedMember.Global

// ReSharper disable UseStringInterpolation

// ReSharper disable MemberCanBePrivate.Global

#nullable disable
namespace SmartImage.App;

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

	public static string AppFolder => Path.GetDirectoryName(ExeLocation);

	public static Version AppVersion => typeof(AppInfo).Assembly.GetName().Version!;

	public static bool IsExeInAppFolder => File.Exists(Path.Combine(AppFolder, NAME_EXE));

	/// <summary>
	///     <c>Null</c> if executable is not in path.
	/// </summary>
	public static string ExeLocation
	{
		get
		{
			var module = Process.GetCurrentProcess().MainModule;

			Require.NotNull(module);

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
		Global.Setup();
		var pathDirectories = FileSystem.GetEnvironmentPathDirectories();

		var oldFolders = pathDirectories.Where(x => x.Contains(NAME) && x != AppFolder);

		foreach (string s in oldFolders) {
			FileSystem.RemoveFromPath(s);
		}

		if (!IsAppFolderInPath) {
			AppIntegration.HandlePath(true);
		}
		
		Debug.WriteLine($"Cli utilities: {AppIntegration.Utilities.QuickJoin()}", C_INFO);
	}
}