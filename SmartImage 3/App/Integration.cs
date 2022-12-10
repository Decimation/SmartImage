#nullable disable

#region

using Kantan.Console;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Kantan.Console.Cli;
using Novus.OS;
using Kantan.Diagnostics;
using Novus.Win32;
using Novus.Win32.Structures.Kernel32;
using Novus.Win32.Structures.User32;
using SmartImage.Lib;
using Terminal.Gui;
using Command = Novus.OS.Command;
using SmartImage.Shell;
using Clipboard = Novus.Win32.Clipboard;

#endregion

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
						regMenu?.SetValue(String.Empty, Resources.Name);
						regMenu?.SetValue("Icon", $"\"{fullPath}\"");

						regCmd = Registry.CurrentUser.CreateSubKey(R1.Reg_Shell_Cmd);

						regCmd?.SetValue(String.Empty,
						                 $"\"{fullPath}\" {Resources.Arg_Input} \"%1\" {R2.Arg_AutoSearch}");
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
		const uint png = (uint)ClipboardFormat.PNG;

		if (Native.IsClipboardFormatAvailable(png))
		{
			var data    = Native.GetClipboardData(png);
			var pngData = ImageUtility.ReadPNG(data);
			i = pngData;
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

		str =(string) Clipboard.GetData((uint) ClipboardFormat.FileNameW);

		if (!SearchQuery.IsUriOrFile(str)) {
			str = (string)  Clipboard.GetData((uint) ClipboardFormat.CF_TEXT);
		}

		if (ReadClipboardImage(out var ms)) {
			var s = Path.Combine(Path.GetTempPath(), $"clipboard_{ms.Length}.png");
			if (!File.Exists(s)) {
				File.WriteAllBytes(s, ms);

			}

			str = s;
			Debug.WriteLine($"read png from clipboard {s}");
		}

		Clipboard.Close();
		// Debug.WriteLine($"Clipboard data: {str}");

		var b = SearchQuery.IsUriOrFile(str);
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