using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static Novus.Win32.Native;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
#pragma warning disable CA1416

namespace SmartImage.Utilities
{
	internal static class NativeImports
	{

		[DllImport(USER32_DLL)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);


		[DllImport(USER32_DLL, EntryPoint = "FindWindow", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern IntPtr FindWindowByCaption(IntPtr zeroOnly, string lpWindowName);


		[DllImport(KERNEL32_DLL, ExactSpelling = true)]
		private static extern IntPtr GetConsoleWindow();

		[DllImport(USER32_DLL)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetForegroundWindow(IntPtr hWnd);

		[StructLayout(LayoutKind.Sequential)]
		private struct FLASHWINFO
		{
			public uint            cbSize;
			public IntPtr          hwnd;
			public FlashWindowType dwFlags;
			public uint            uCount;
			public int             dwTimeout;
		}

		private enum FlashWindowType : uint
		{
			/// <summary>
			/// Stop flashing. The system restores the window to its original state.
			/// </summary>    
			FLASHW_STOP = 0,

			/// <summary>
			/// Flash the window caption
			/// </summary>
			FLASHW_CAPTION = 1,

			/// <summary>
			/// Flash the taskbar button.
			/// </summary>
			FLASHW_TRAY = 2,

			/// <summary>
			/// Flash both the window caption and taskbar button.
			/// This is equivalent to setting the <see cref="FLASHW_CAPTION"/> | <see cref="FLASHW_TRAY"/> flags.
			/// </summary>
			FLASHW_ALL = 3,

			/// <summary>
			/// Flash continuously, until the <seealso cref="FLASHW_STOP"/> flag is set.
			/// </summary>
			FLASHW_TIMER = 4,

			/// <summary>
			/// Flash continuously until the window comes to the foreground.
			/// </summary>
			FLASHW_TIMERNOFG = 12
		}

		internal static void FlashWindow(IntPtr hWnd)
		{
			var fInfo = new FLASHWINFO();

			fInfo.cbSize    = Convert.ToUInt32(Marshal.SizeOf(fInfo));
			fInfo.hwnd      = hWnd;
			fInfo.dwFlags   = FlashWindowType.FLASHW_ALL;
			fInfo.uCount    = 8;
			fInfo.dwTimeout = 75;

			FlashWindowEx(ref fInfo);
		}


		/// <summary>
		/// Gets console application's window handle. <see cref="Process.MainWindowHandle"/> does not work in some cases.
		/// </summary>
		internal static IntPtr GetConsoleWindowHandle() => FindWindowByCaption(IntPtr.Zero, Console.Title);

		internal static void FlashConsoleWindow() => FlashWindow(GetConsoleWindowHandle());

		internal static void BringConsoleToFront() => SetForegroundWindow(GetConsoleWindowHandle());
	}
}