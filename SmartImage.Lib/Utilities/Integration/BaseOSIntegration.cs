// Author: Deci | Project: SmartImage.Lib | Name: BaseOSIntegration.cs
// Date: 2024/12/05 @ 21:12:49

using System.Diagnostics;
using Novus.OS;

// ReSharper disable InconsistentNaming

namespace SmartImage.Lib.Utilities.Integration;

public abstract class BaseOSIntegration
{

	#region

	public virtual bool IsRoot => FileSystem.IsRoot;

	public abstract bool IsContextMenuAdded { get; }

	public abstract string ProgramFilesPath { get; }

	public abstract string AppDataPath { get; }

	public virtual string PersonalPath { get; } = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

	public abstract string LaunchArgs { get; }

	[CBN]
	public abstract string ChromePath { get; }

	[CBN]
	public abstract string FirefoxPath { get; }


	[MNNW(true, nameof(ChromePath))]
	public bool IsChromeInstalled => Path.Exists(ChromePath);

	[MNNW(true, nameof(FirefoxPath))]
	public bool IsFirefoxInstalled => Path.Exists(FirefoxPath);

	#endregion

	static BaseOSIntegration()
	{
		Executable          = GetProcessMainModuleFileName();
		ExecutableDirectory = Path.GetDirectoryName(Executable);

		if (FileSystem.IsWindows) {
			Integration = new WindowsOSIntegration();
		}
		else if (FileSystem.IsLinux) {
			Integration = new LinuxOSIntegration();
		}
		else {
			Integration = null;
			throw new NotSupportedException("OS not supported");
		}
	}

	#region

	public const int EC_ERROR = -1;

	public const int EC_OK = 0;


	public static BaseOSIntegration Integration { get; }

	public static string ExecutableDirectory {get;}

	public static bool IsExecutableInPath
		=> FileSystem.IsFolderInPath(ExecutableDirectory);

	public static string Executable {get;}

	#endregion

	/// <returns><c>true</c> if operation succeeded; <c>false</c> otherwise</returns>
	public abstract bool? AddToPath(bool option);

	/// <returns><c>true</c> if operation succeeded; <c>false</c> otherwise</returns>
	public abstract bool? HandleContextMenu(bool option, string args);

	public abstract void FlashNotify(nint fd);

	public static string GetProcessMainModuleFileName()
	{
		// TODO: vs Directory.GetCurrentDirectory

		ProcessModule module = Process.GetCurrentProcess().MainModule;

		// Require.NotNull(module);
		Trace.Assert(module != null);
		return module.FileName;
	}

}