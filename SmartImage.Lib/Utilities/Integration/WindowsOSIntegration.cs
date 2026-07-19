// Author: Deci | Project: SmartImage.Lib | Name: WindowsOSIntegration.cs
// Date: 2024/12/05 @ 21:12:18

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using Novus.OS;
using Novus.Win32;
using Novus.Win32.Structures.User32;
using SmartImage.Lib.Images.Alloc;
using SmartImage.Shared;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace SmartImage.Lib.Utilities.Integration;

[SupportedOSPlatform(Common.OS_WIN)]
public sealed class WindowsOSIntegration : BaseOSIntegration
{

	[NN]
	public override string ChromePath => Path.Combine(ProgramFilesPath, @"Google\Chrome\Application\chrome.exe");

	[NN]
	public override string FirefoxPath => Path.Combine(AppDataPath, @"Mozilla");

	public override string LaunchArgs { get; } = R1.Reg_Launch_Args;

	public override string ProgramFilesPath { get; } = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

	public override string AppDataPath { get; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

	public override string GalleryDLPath { get => FileSystem.SearchInEnvironmentPath(GALLERY_DL_EXE); }

	public override bool IsContextMenuAdded
	{
		get
		{
			using var reg = Registry.CurrentUser.OpenSubKey(R1.Reg_Shell_Cmd);
			return reg != null;
		}
	}

	public override bool? AddToPath(bool option)
	{
		if (option) {
			var p = FileSystem.GetEnvironmentPath();
			FileSystem.SetEnvironmentPath(p + $";{ExecutableDirectory}");
		}
		else {
			FileSystem.RemoveFromPath(ExecutableDirectory);
		}

		return true;
	}

	/*
	 * HKEY_CLASSES_ROOT is an alias, a merging, of two other locations:
	 *		HKEY_CURRENT_USER\Software\Classes
	 *		HKEY_LOCAL_MACHINE\Software\Classes
	 */


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

				string fullPath = Environment.ProcessPath;

				try {
					regMenu = Registry.CurrentUser.CreateSubKey(R1.Reg_Shell);
					regMenu?.SetValue(String.Empty, R1.Name);
					regMenu?.SetValue("Icon", $"\"{fullPath}\"");

					regCmd = Registry.CurrentUser.CreateSubKey(R1.Reg_Shell_Cmd);

					regCmd?.SetValue(String.Empty, $"\"{fullPath}\" {args}");

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
				}
				catch (Exception ex) {
					Trace.WriteLine($"{ex.Message}");
					ok = false;

					// return false;
				}

				break;

		}

		return ok;
	}

	public override void FlashNotify(nint hwnd)
	{
		var pwfi = new FLASHWINFO
		{
			cbSize    = (uint) Marshal.SizeOf<FLASHWINFO>(),
			hwnd      = hwnd,
			dwFlags   = FlashWindowType.FLASHW_TRAY,
			uCount    = 8,
			dwTimeout = 75
		};

		Native.FlashWindowEx(ref pwfi);
	}

	[CBN]
	public static string TryGetClipboardData()
	{
		Clipboard.Open();

		string data = null;
		
		ClipboardFormat[] imgFormats = [ClipboardFormat.PNG, ClipboardFormat.PNG2,ClipboardFormat.PNG3, ClipboardFormat.BMP2];

		if (Clipboard.IsFormatAvailable((uint)(ClipboardFormat.CF_TEXT))) {
			data = (string) Clipboard.GetData((uint)(ClipboardFormat.CF_TEXT));
		}
		else if (Clipboard.IsFormatAvailable((uint) ClipboardFormat.CF_HDROP)) {
			data = Clipboard.GetDragQueryList()?.FirstOrDefault();
		}

		var format = imgFormats.FirstOrDefault(f=>Clipboard.IsFormatAvailable((uint) f));

		if (format != default) {
			var    ptr          = (byte[]) Clipboard.GetData((uint)(format));
			var tempFileName = Path.ChangeExtension(Path.GetTempFileName(), "png");
			File.WriteAllBytes(tempFileName, ptr);
			data = tempFileName;
		}

		if (AllocImage.IsValidSourceType(data)) {
			
		}

		Clipboard.Close();

		return data;
	}

}