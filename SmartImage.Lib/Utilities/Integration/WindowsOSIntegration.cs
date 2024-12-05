// Author: Deci | Project: SmartImage.Lib | Name: WindowsOSIntegration.cs
// Date: 2024/12/04 @ 22:12:41

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using Novus.OS;
using Novus.Win32;
using Novus.Win32.Structures.User32;

// ReSharper disable InconsistentNaming

namespace SmartImage.Lib.Utilities.Integration;

[SupportedOSPlatform(OS_WIN)]
public sealed class WindowsOSIntegration : BaseOSIntegration
{

	public override string ChromePath => Path.Combine(ProgramFilesPath, @"Google\Chrome\Application\chrome.exe");

	public override string FirefoxPath => Path.Combine(AppDataPath, @"Mozilla");

	public override bool? AddToPath(bool option)
	{
		if (option) {
			var p = FileSystem.GetEnvironmentPath();
			FileSystem.SetEnvironmentPath(p + $";{CurrentAppFolder}");

		}
		else {
			FileSystem.RemoveFromPath(CurrentAppFolder);
		}

		return true;
	}

	#region Overrides of BaseOSIntegration

	public override string LaunchArgs { get; } = R1.Reg_Launch_Args;

	#endregion

	public override string ProgramFilesPath { get; } =
		Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

	public override string AppDataPath { get; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

	public override bool? HandleContextMenu(bool option, string args)
	{
		/*
		 * New context menu
		 */
		bool ok = false;

		switch (option) {
			case true:

				args ??= R1.Reg_Launch_Args;

				RegistryKey regMenu = null;
				RegistryKey regCmd  = null;

				string fullPath = ExeLocation;

				try {
					regMenu = Registry.CurrentUser.CreateSubKey(R1.Reg_Shell);
					regMenu?.SetValue(string.Empty, R1.Name);
					regMenu?.SetValue("Icon", $"\"{fullPath}\"");

					regCmd = Registry.CurrentUser.CreateSubKey(R1.Reg_Shell_Cmd);

					regCmd?.SetValue(string.Empty, $"\"{fullPath}\" {args}");

					// regCmd?.SetValue(String.Empty, $"\"{fullPath}\" \"%1\"");
					// regCmd?.SetValue(String.Empty, $"\"{fullPath}\" -i \"%1\" -auto -s");
					ok = true;
				}
				catch (Exception ex) {
					Trace.WriteLine($"{ex.Message}");

					// return false;
					ok = false;
				}
				finally {
					regMenu?.Close();
					regCmd?.Close();
				}

				break;

			case false:

				try {
					var reg = Registry.CurrentUser.OpenSubKey(R1.Reg_Shell_Cmd);

					if (reg != null) {
						reg.Close();
						Registry.CurrentUser.DeleteSubKey(R1.Reg_Shell_Cmd);
					}

					reg = Registry.CurrentUser.OpenSubKey(R1.Reg_Shell);

					if (reg != null) {
						reg.Close();
						Registry.CurrentUser.DeleteSubKey(R1.Reg_Shell);
					}

					// return true;
					ok = true;
					break;
				}
				catch (Exception ex) {
					Trace.WriteLine($"{ex.Message}");
					ok = false;

					// return false;
					break;
				}

				break;

		}

		return ok;
	}


	public override bool IsContextMenuAdded
	{
		get
		{
			using var reg = Registry.CurrentUser.OpenSubKey(R1.Reg_Shell_Cmd);
			return reg != null;

		}
	}

	public override void FlashNotify(nint hwnd)
	{
		var pwfi = new FLASHWINFO()
		{
			cbSize    = (uint) Marshal.SizeOf<FLASHWINFO>(),
			hwnd      = hwnd,
			dwFlags   = FlashWindowType.FLASHW_TRAY,
			uCount    = 8,
			dwTimeout = 75
		};

		Native.FlashWindowEx(ref pwfi);
	}

}