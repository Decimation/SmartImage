// Read S SmartImage Compat.cs
// 2022-12-03 @ 11:04 AM

#region

using System.Runtime.Versioning;
using Novus;

#endregion

namespace SmartImage.App;

internal static class Compat
{
	internal const string OS = $"{Global.OS_WIN}";

	[field: SupportedOSPlatformGuard(OS)]
	internal static bool IsWin = OperatingSystem.IsWindows();
}