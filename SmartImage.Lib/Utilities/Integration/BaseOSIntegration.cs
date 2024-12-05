// Author: Deci | Project: SmartImage.Lib | Name: BaseOSIntegration.cs
// Date: 2024/12/04 @ 21:12:39

using System.Diagnostics;
using System.Runtime.Versioning;
using Novus.OS;

// ReSharper disable InconsistentNaming

namespace SmartImage.Lib.Utilities.Integration;

public abstract class BaseOSIntegration
{

	public virtual bool IsRoot => FileSystem.IsRoot;

	public abstract bool IsContextMenuAdded { get; }

	public abstract string ProgramFilesPath { get; }

	public abstract string AppDataPath { get; }

	public static BaseOSIntegration Integration { get; }

	public static string CurrentAppFolder
		=> Path.GetDirectoryName(ExeLocation);

	public static bool IsAppFolderInPath
		=> FileSystem.IsFolderInPath(CurrentAppFolder);

	internal const string OS_WIN = "windows";

	internal const string OS_LINUX = "linux";

	public const int EC_ERROR = -1;

	public const int EC_OK = 0;

	[SupportedOSPlatformGuard(OS_LINUX)]
	public static readonly bool IsLinux = OperatingSystem.IsLinux();

	[SupportedOSPlatformGuard(OS_WIN)]
	public static readonly bool IsWindows = OperatingSystem.IsWindows();

	public static readonly string ExeLocation;

	public abstract string LaunchArgs { get; }

	static BaseOSIntegration()
	{
		ExeLocation = GetProcessMainModuleFileName();

		if (IsWindows) {
			Integration = new WindowsOSIntegration();
		}
		else if (IsLinux) {
			Integration = new LinuxOSIntegration();
		}
		else {
			Integration = null;
			throw new NotSupportedException($"OS not supported");
		}
	}

	/// <returns><c>true</c> if operation succeeded; <c>false</c> otherwise</returns>
	public abstract bool? AddToPath(bool option);

	/// <returns><c>true</c> if operation succeeded; <c>false</c> otherwise</returns>
	public abstract bool? HandleContextMenu(bool option, string args);

	public abstract void FlashNotify(nint fd);

	public static string GetProcessMainModuleFileName()
	{
		// TODO: vs Directory.GetCurrentDirectory

		var module = Process.GetCurrentProcess().MainModule;

		// Require.NotNull(module);
		Trace.Assert(module != null);
		return module.FileName;
	}

	[CBN]
	public abstract string ChromePath { get; }

	[CBN]
	public abstract string FirefoxPath { get; }


	[MNNW(true, nameof(ChromePath))]
	public bool IsChromeInstalled => Path.Exists(ChromePath);

	[MNNW(true, nameof(FirefoxPath))]
	public bool IsFirefoxInstalled => Path.Exists(FirefoxPath);

}