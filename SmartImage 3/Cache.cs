using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

	internal static readonly IntPtr StdOut = Native.GetStdHandle(StandardHandle.STD_OUTPUT_HANDLE);
	internal static readonly IntPtr StdIn  = Native.GetStdHandle(StandardHandle.STD_INPUT_HANDLE);

	private static ConsoleModes _oldMode;

	internal static void SetConsoleMenu()
	{
		IntPtr sysMenu = Native.GetSystemMenu(HndWindow, false);

		Native.DeleteMenu(sysMenu, (int) SysCommand.SC_MAXIMIZE, (int) Native.MF_BYCOMMAND);
		Native.DeleteMenu(sysMenu, (int) SysCommand.SC_SIZE, (int) Native.MF_BYCOMMAND);
	}

	internal static void SetConsoleMode()
	{
		Console.InputEncoding = Console.OutputEncoding = Encoding.UTF8;
		Native.GetConsoleMode(Cache.StdIn, out ConsoleModes lpMode);

		Cache._oldMode = lpMode;

		Native.SetConsoleMode(Cache.StdIn, lpMode | ((ConsoleModes.ENABLE_MOUSE_INPUT &
		                                              ~ConsoleModes.ENABLE_QUICK_EDIT_MODE) |
		                                             ConsoleModes.ENABLE_EXTENDED_FLAGS |
		                                             ConsoleModes.ENABLE_ECHO_INPUT |
		                                             ConsoleModes.ENABLE_VIRTUAL_TERMINAL_PROCESSING));
	}
}