using System.Text;
using Novus.Win32;
using Novus.Win32.Structures.Kernel32;
using SmartImage.Lib.Engines;

namespace SmartImage.Shell;

internal static class ConsoleUtil
{
    internal static readonly SearchEngineOptions[] EngineOptions = Enum.GetValues<SearchEngineOptions>();

    internal static readonly IntPtr HndWindow = Native.GetConsoleWindow();
    internal static readonly IntPtr StdOut = Native.GetStdHandle(StandardHandle.STD_OUTPUT_HANDLE);
    internal static readonly IntPtr StdIn = Native.GetStdHandle(StandardHandle.STD_INPUT_HANDLE);

    internal static ConsoleModes _oldMode;

    internal static void SetConsoleMenu()
    {
        IntPtr sysMenu = Native.GetSystemMenu(HndWindow, false);

        Native.DeleteMenu(sysMenu, (int)SysCommand.SC_MAXIMIZE, Native.MF_BYCOMMAND);
        Native.DeleteMenu(sysMenu, (int)SysCommand.SC_SIZE, Native.MF_BYCOMMAND);
    }

    internal static void SetConsoleMode()
    {
        Native.OpenClipboard();

        Console.InputEncoding = Console.OutputEncoding = Encoding.UTF8;
        Native.GetConsoleMode(StdIn, out ConsoleModes lpMode);

        _oldMode = lpMode;

        Native.SetConsoleMode(StdIn, lpMode | ConsoleModes.ENABLE_MOUSE_INPUT &
                                                            ~ConsoleModes.ENABLE_QUICK_EDIT_MODE |
                                                           ConsoleModes.ENABLE_EXTENDED_FLAGS |
                                                           ConsoleModes.ENABLE_ECHO_INPUT |
                                                           ConsoleModes.ENABLE_VIRTUAL_TERMINAL_PROCESSING);
    }
}