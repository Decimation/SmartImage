using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Novus.FileTypes;
using Novus.Win32;
using Novus.Win32.Structures.Kernel32;
using SmartImage.Lib;

namespace SmartImage;

internal static class Cache
{
	internal static readonly SearchEngineOptions[] EngineOptions = Enum.GetValues<SearchEngineOptions>();

	internal static readonly Func<SearchEngineOptions, SearchEngineOptions, SearchEngineOptions> EnumAggregator =
		(current, searchEngineOptions) => current | searchEngineOptions;

	internal static readonly IntPtr HndWindow = Native.GetConsoleWindow();
	internal static readonly IntPtr StdOut    = Native.GetStdHandle(StandardHandle.STD_OUTPUT_HANDLE);
	internal static readonly IntPtr StdIn     = Native.GetStdHandle(StandardHandle.STD_INPUT_HANDLE);

	internal static ConsoleModes _oldMode;

	internal static QFileInfo _clipboard;

	public static void SetConsoleMenu()
	{
		IntPtr sysMenu = Native.GetSystemMenu(HndWindow, false);
		
		Native.DeleteMenu(sysMenu, (int) SysCommand.SC_MAXIMIZE, (int) Native.MF_BYCOMMAND);
		Native.DeleteMenu(sysMenu, (int) SysCommand.SC_SIZE, (int) Native.MF_BYCOMMAND);
	}
}