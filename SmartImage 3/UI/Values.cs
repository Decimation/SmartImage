using Novus.Win32;
using Novus.Win32.Structures.Kernel32;
using NStack;
using SmartImage.Lib;
using Terminal.Gui;

// ReSharper disable InconsistentNaming

namespace SmartImage.UI;

internal static class Values
{
	internal static readonly ustring Err = ustring.Make('!');
	internal static readonly ustring NA  = ustring.Make(Application.Driver.RightDefaultIndicator);
	internal static readonly ustring OK  = ustring.Make(Application.Driver.Checked);
	internal static readonly ustring PRC = ustring.Make(Application.Driver.Diamond);

	internal static readonly SearchEngineOptions[] EngineOptions = Enum.GetValues<SearchEngineOptions>();

	internal static readonly IntPtr HndWindow = Native.GetConsoleWindow();
	internal static readonly IntPtr StdOut    = Native.GetStdHandle(StandardHandle.STD_OUTPUT_HANDLE);
	internal static readonly IntPtr StdIn     = Native.GetStdHandle(StandardHandle.STD_INPUT_HANDLE);

	internal static ConsoleModes _oldMode;
}