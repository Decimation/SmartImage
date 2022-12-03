using System.Runtime.Versioning;
using Novus;

namespace SmartImage.App;

internal static class Compat
{
	[field: SupportedOSPlatformGuard(OS)]
	internal static bool IsWin = OperatingSystem.IsWindows();

	internal const string OS = $"{Global.OS_WIN}";
}