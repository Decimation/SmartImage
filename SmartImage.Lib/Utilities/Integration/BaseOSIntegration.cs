// Author: Deci | Project: SmartImage.Lib | Name: BaseOSIntegration.cs
// Date: 2024/12/05 @ 21:12:49

using System.Diagnostics;
using System.Runtime.Versioning;
using Novus.OS;

// ReSharper disable InconsistentNaming

namespace SmartImage.Lib.Utilities.Integration;

public abstract class BaseOSIntegration
{

#region

	protected BaseOSIntegration()
	{
		PersonalPath  = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
		GalleryDLPath = FileSystem.FindInPath(IsWindows ? GALLERY_DL_EXE : GALLERY_DL);
	}

	public virtual bool IsRoot => FileSystem.IsRoot;

	public abstract bool IsContextMenuAdded { get; }

	public abstract string ProgramFilesPath { get; }

	public abstract string AppDataPath { get; }

	public virtual string PersonalPath { get; }

	public abstract string LaunchArgs { get; }

	[CBN]
	public abstract string ChromePath { get; }

	[MNNW(true, nameof(ChromePath))]
	public bool IsChromeInstalled => Path.Exists(ChromePath);

	[CBN]
	public abstract string FirefoxPath { get; }

	[MNNW(true, nameof(FirefoxPath))]
	public bool IsFirefoxInstalled => Path.Exists(FirefoxPath);

	[CBN]
	public virtual string GalleryDLPath { get; }

	[MNNW(true, nameof(GalleryDLPath))]
	public bool IsGalleryDLInstalled => Path.Exists(GalleryDLPath);

#endregion

	static BaseOSIntegration()
	{
		Executable          = GetProcessMainModuleFileName();
		ExecutableDirectory = Path.GetDirectoryName(Executable);
		IsExecutableInPath  = FileSystem.IsFolderInPath(ExecutableDirectory);

		if (IsWindows) {
			Integration = new WindowsOSIntegration();
		}
		else if (IsLinux) {
			Integration = new LinuxOSIntegration();
		}
		else {
			Integration = null;
		}
	}

#region

	[SupportedOSPlatformGuard(Common.OS_LINUX)]
	public static readonly bool IsLinux = OperatingSystem.IsLinux();

	[SupportedOSPlatformGuard(Common.OS_WIN)]
	public static readonly bool IsWindows = OperatingSystem.IsWindows();

	public static readonly string ExecutableDirectory;

	public static readonly bool IsExecutableInPath;

	public static readonly string Executable;

	public static BaseOSIntegration Integration { get; }

#endregion

	/// <returns><c>true</c> if operation succeeded; <c>false</c> otherwise</returns>
	public abstract bool? AddToPath(bool option);

	/// <returns><c>true</c> if operation succeeded; <c>false</c> otherwise</returns>
	public abstract bool? HandleContextMenu(bool option, string args);

	public abstract void FlashNotify(nint fd);

	public static string GetProcessMainModuleFileName()
	{
		// TODO: vs Directory.GetCurrentDirectory?

		ProcessModule module = Process.GetCurrentProcess().MainModule;

		// Require.NotNull(module);
		Trace.Assert(module != null);

		return module.FileName;
	}

	public const string GALLERY_DL     = "gallery-dl";
	public const string GALLERY_DL_EXE = $"{GALLERY_DL}.exe";

}