using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Kantan.Console;
using Novus;
using Novus.Win32;
using Novus.Win32.Structures.Kernel32;
using Novus.Win32.Structures.User32;
using SmartImage.Lib.Engines;
using Terminal.Gui;

namespace SmartImage.Shell;

internal static class ConsoleUtil
{

	// internal static SearchEngineOptions[] EngineOptions => (SearchEngineOptions[]) Cache[nameof(EngineOptions)];
	internal static SearchEngineOptions[] EngineOptions = Enum.GetValues<SearchEngineOptions>();

	internal static readonly nint HndWindow = Native.GetConsoleWindow();
	internal static readonly nint StdOut    = Native.GetStdHandle(StandardHandle.STD_OUTPUT_HANDLE);
	internal static readonly nint StdIn     = Native.GetStdHandle(StandardHandle.STD_INPUT_HANDLE);

	internal static ConsoleModes _oldMode;

	static ConsoleUtil()
	{
		// Cache[nameof(EngineOptions)] = Enum.GetValues<SearchEngineOptions>();
	}

	internal static void SetConsoleMenu()
	{
		nint sysMenu = Native.GetSystemMenu(HndWindow, false);

		Native.DeleteMenu(sysMenu, (int) SysCommand.SC_MAXIMIZE, Native.MF_BYCOMMAND);
		Native.DeleteMenu(sysMenu, (int) SysCommand.SC_SIZE, Native.MF_BYCOMMAND);
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
		Console.SetWindowSize(150, 35);
		Console.BufferWidth = 150;

	}

	[field: SupportedOSPlatformGuard(Global.OS_WIN)]
	internal static bool _isWin = OperatingSystem.IsWindows();

	internal static void FlashTaskbar()
	{
		var pwfi = new FLASHWINFO()
		{
			cbSize    = (uint) Marshal.SizeOf<FLASHWINFO>(),
			hwnd      = ConsoleUtil.HndWindow,
			dwFlags   = FlashWindowType.FLASHW_TRAY,
			uCount    = 8,
			dwTimeout = 75
		};

		Native.FlashWindowEx(ref pwfi);
	}

	internal static bool QueueProgress(CancellationTokenSource cts, ProgressBar pbr, Action<object>? f = null)
	{
		return ThreadPool.QueueUserWorkItem((state) =>
		{
			while (state is CancellationToken { IsCancellationRequested: false }) {
				pbr.Pulse();
				f?.Invoke(state);
				// Thread.Sleep(TimeSpan.FromMilliseconds(100));
			}

		}, cts.Token);
	}

	internal static void OnEngineSelected(ListViewItemEventArgs args, ref SearchEngineOptions e, ListView lv)
	{
		var val = (SearchEngineOptions) args.Value;

		var isMarked = lv.Source.IsMarked(args.Item);

		bool b = val == SearchEngineOptions.None;

		if (isMarked) {
			if (b) {
				e = val;

				for (int i = 1; i < lv.Source.Length; i++) {
					lv.Source.SetMark(i, false);
				}
			}
			else {
				e |= val;
			}
		}
		else {
			e &= ~val;
		}

		if (!b) {
			lv.Source.SetMark(0, false);
		}

		lv.FromEnum(e);

		ret:
		lv.SetNeedsDisplay();
		Debug.WriteLine($"{val} {args.Item} -> {e} {isMarked}", nameof(OnEngineSelected));
	}
}