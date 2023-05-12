#nullable disable

#region

using Microsoft.Win32;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Novus.OS;
using Novus;
using Novus.Win32;
using Novus.Win32.Structures.User32;
using SmartImage.Lib;
using Command = Novus.OS.Command;
using Clipboard = Novus.Win32.Clipboard;
using SmartImage.Utilities;

#endregion

// TODO: cross-platform compatibility

namespace SmartImage.App;

/// <summary>
///     Program OS integrations
/// </summary>
public static class Integration
{
	public static string ExeLocation
	{
		get
		{
			var module = Process.GetCurrentProcess().MainModule;

			// Require.NotNull(module);
			Trace.Assert(module != null);
			return module.FileName;
		}
	}

	public static Version Version => Assembly.GetExecutingAssembly().GetName().Version;

	public static string CurrentAppFolder => Path.GetDirectoryName(ExeLocation);

	public static bool IsAppFolderInPath => FileSystem.IsFolderInPath(CurrentAppFolder);

	public static bool IsOnTop { get; private set; }

	public static bool IsContextMenuAdded
	{
		get
		{
			if (OperatingSystem.IsWindows()) {
				var reg = Registry.CurrentUser.OpenSubKey(R1.Reg_Shell_Cmd);
				return reg != null;

			}

			return false;
		}
	}
	/*
	 * HKEY_CLASSES_ROOT is an alias, a merging, of two other locations:
	 *		HKEY_CURRENT_USER\Software\Classes
	 *		HKEY_LOCAL_MACHINE\Software\Classes
	 */

	/// <returns><c>true</c> if operation succeeded; <c>false</c> otherwise</returns>
	public static bool HandleContextMenu(bool option)
	{
		/*
		 * New context menu
		 */
		if (OperatingSystem.IsWindows()) {
			switch (option) {
				case true:

					RegistryKey regMenu = null;
					RegistryKey regCmd  = null;

					string fullPath = ExeLocation;

					try {
						regMenu = Registry.CurrentUser.CreateSubKey(R1.Reg_Shell);
						regMenu?.SetValue(String.Empty, R2.Name);
						regMenu?.SetValue("Icon", $"\"{fullPath}\"");

						regCmd = Registry.CurrentUser.CreateSubKey(R1.Reg_Shell_Cmd);

						regCmd?.SetValue(String.Empty,
						                 $"\"{fullPath}\" {R2.Arg_Input} \"%1\" {R2.Arg_AutoSearch}");
					}
					catch (Exception ex) {
						Trace.WriteLine($"{ex.Message}");
						return false;
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
					}
					catch (Exception ex) {
						Trace.WriteLine($"{ex.Message}", C_ERROR);

						return false;
					}

					break;

			}

		}

		return false;

	}

	public static void HandlePath(bool option)
	{
		switch (option) {
			case true:
			{
				string oldValue  = FileSystem.GetEnvironmentPath();
				string appFolder = CurrentAppFolder;

				if (IsAppFolderInPath) {
					return;
				}

				bool appFolderInPath = oldValue
					.Split(FileSystem.PATH_DELIM)
					.Any(p => p == appFolder);

				string cd  = Environment.CurrentDirectory;
				string exe = Path.Combine(cd, ExeLocation);

				if (!appFolderInPath) {
					string newValue = oldValue + FileSystem.PATH_DELIM + cd;
					FileSystem.SetEnvironmentPath(newValue);
				}

				break;
			}
			case false:
				FileSystem.RemoveFromPath(CurrentAppFolder);
				break;
		}
	}

	public static void Reset()
	{
		// Computer\HKEY_CLASSES_ROOT\*\shell\SmartImage

		if (IsContextMenuAdded) {
			if (OperatingSystem.IsWindows()) {
				HandleContextMenu(false);

			}
		}

	}

	[DoesNotReturn]
	public static void Uninstall()
	{
		// autonomous uninstall routine

		// self destruct

		string exeFileName = ExeLocation;

		const string DEL_BAT_NAME = "SmartImage_Delete.bat";

		string[] commands =
		{
			"@echo off",

			/* Wait approximately 4 seconds (so that the process is already terminated) */
			"ping 127.0.0.1 > nul",

			/* Delete executable */
			$"echo y | del /F {exeFileName}",

			/* Delete this bat file */
			$"echo y | del {DEL_BAT_NAME}"
		};

		// Runs in background
		var proc = Command.Batch(commands, DEL_BAT_NAME);
		proc.Start();

	}

	[SupportedOSPlatform(Global.OS_WIN)]
	public static void KeepOnTop(bool add)
	{
		if (add) {
			Native.KeepWindowOnTop(ConsoleUtil.HndWindow);
		}
		else {
			Native.RemoveWindowOnTop(ConsoleUtil.HndWindow);
		}

		IsOnTop = add;
	}

	public static bool ReadClipboardImage(out byte[] i)
	{
		const uint png = (uint) ClipboardFormat.PNG;

		// var        sb = new StringBuilder(2048);
		// var l=Native.GetClipboardFormatName(png, sb, sb.Length);
		// Debug.WriteLine($"{sb} {l}");
		if (Clipboard.IsFormatAvailable(png)) {
			i = Clipboard.GetData(png) as byte[];
			return true;
		}
		else {
			i = null;
			return false;
		}
	}

	public static bool ReadClipboard(out string str)
	{
		Clipboard.Open();

		/*
		 * 1	File
		 */

		var data = Clipboard.GetData((uint) ClipboardFormat.FileNameW);

		if (data is IntPtr { } p && p == IntPtr.Zero) {
			str = null;
		}
		else {
			str = (string) data;
		}

		if (!string.IsNullOrWhiteSpace(str)) goto cl;

		/*
		 * 3	Text
		 */

		if (!SearchQuery.IsValidSourceType(str)) {
			var o = Clipboard.GetData((uint) ClipboardFormat.CF_UNICODETEXT);

			if ((data is IntPtr { } p2 && p2 == IntPtr.Zero) || o is IntPtr data2 && data2 == IntPtr.Zero) {
				str = null;
			}
			else {
				str = (string) o;
			}

			if (o is nint n && n != nint.Zero) {
				// str = (string) o;

				str = Marshal.PtrToStringUni(n);
			}

			if (!string.IsNullOrWhiteSpace(str)) goto cl;
		}

		/*
		 * 3	Screenshot
		 */

		if (ReadClipboardImage(out var ms)) {
			//todo: delete on exit
			var s = Path.Combine(Path.GetTempPath(), $"clipboard_{ms.Length}.png");

			if (!File.Exists(s)) {
				File.WriteAllBytes(s, ms);
			}

			str = s;
			Debug.WriteLine($"read png from clipboard {s}");
		}

		cl:
		Clipboard.Close();
		Debug.WriteLine($"Clipboard data: {str}");

		var b = SearchQuery.IsValidSourceType(str);
		return b;
	}

	public static string OpenFile()
	{
		var ofn = new OPENFILENAME()
		{
			lStructSize = Marshal.SizeOf<OPENFILENAME>(),
			lpstrFilter = "All Files\0*.*\0\0",

			lpstrFile       = new string(stackalloc char[256]),
			lpstrFileTitle  = new string(stackalloc char[64]),
			lpstrInitialDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonPictures),
			lpstrTitle      = "Pick an image"
		};
		ofn.nMaxFile      = ofn.lpstrFile.Length;
		ofn.nMaxFileTitle = ofn.lpstrFileTitle.Length;

		if (Native.GetOpenFileName(ref ofn)) {
			return ofn.lpstrFile;
		}

		return null;
	}
}