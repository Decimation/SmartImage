// Author: Deci | Project: SmartImage.Rdx | Name: AppUtil.cs
// Date: 2024/05/22 @ 15:05:58

#nullable disable
global using USI=JetBrains.Annotations.UsedImplicitlyAttribute;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text.Json.Serialization;
using Flurl.Http;
using JetBrains.Annotations;
using Kantan.Net.Utilities;
using Microsoft.Win32;
using Novus.OS;
using Novus.Win32;
using Novus.Win32.Structures.User32;

// ReSharper disable InconsistentNaming

namespace SmartImage.Lib.Utilities;

public static class AppUtil
{

	internal const string OS_WIN   = "windows";
	internal const string OS_LINUX = "linux";

	[SupportedOSPlatformGuard(OS_LINUX)]
	internal static readonly bool IsLinux = OperatingSystem.IsLinux();

	[SupportedOSPlatformGuard(OS_WIN)]
	internal static readonly bool IsWindows = OperatingSystem.IsWindows();

	public static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

	public static readonly Version Version = Assembly.GetName().Version;

	[CBN]
	internal static string GetOSName()
	{
		string os = null;

		if (IsLinux) {
			os = "Linux";

		}
		else if (IsWindows) {
			os = "Windows";
		}

		return os;
	}

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

	[MN]
	public static string CurrentAppFolder => Path.GetDirectoryName(ExeLocation);

	public static bool IsAppFolderInPath
	{
		get { return FileSystem.IsFolderInPath(CurrentAppFolder); }
	}

	public static bool IsOnTop { get; private set; }

	[SupportedOSPlatform(OS_WIN)]
	internal static void FlashTaskbar(nint hndw)
	{
		var pwfi = new FLASHWINFO()
		{
			cbSize    = (uint) Marshal.SizeOf<FLASHWINFO>(),
			hwnd      = hndw,
			dwFlags   = FlashWindowType.FLASHW_TRAY,
			uCount    = 8,
			dwTimeout = 75
		};

		Native.FlashWindowEx(ref pwfi);
	}

	public static void AddToPath(bool b)
	{

		if (b) {
			var p = FileSystem.GetEnvironmentPath();
			FileSystem.SetEnvironmentPath(p + $";{CurrentAppFolder}");

		}
		else {
			FileSystem.RemoveFromPath(CurrentAppFolder);

		}
	}

	public static bool IsContextMenuAdded
	{
		get
		{
			if (IsWindows) {
				var reg = Registry.CurrentUser.OpenSubKey(R1.Reg_Shell_Cmd);
				return reg != null;
			}
			else if (IsLinux) {
				return File.Exists(LinuxDesktopFile);
			}

			throw new InvalidOperationException();
		}
	}

	/// <returns><c>true</c> if operation succeeded; <c>false</c> otherwise</returns>
	public static bool HandleContextMenu(bool option)
	{
		if (IsWindows) {
			HandleContextMenuWindows(option);
		}
		else if (IsLinux) {
			HandleContextMenuLinux(option);
		}

		throw new InvalidOperationException();
	}

	[SupportedOSPlatform(OS_LINUX)]
	public static bool HandleContextMenuLinux(bool option)
	{
		if (option) {
			string dsk = $"""
			              [Desktop Entry]

			              Type=Application
			              Version=1.0
			              Name=SmartImage
			              Terminal=true
			              Exec={ExeLocation} %u
			              """;
			File.WriteAllText(LinuxDesktopFile, dsk);

		}
		else {
			
			File.Delete(LinuxDesktopFile);
		}

		// Console.WriteLine(Path.GetFullPath(s));
		// Console.ReadLine();

		// File.WriteAllText("~/.local/share/nautilus/scripts/smartimage.desktop", dsk);

		return true;
	}

	[SupportedOSPlatform(OS_WIN)]
	public static bool HandleContextMenuWindows(bool option)
	{
		/*
		 * New context menu
		 */
		switch (option) {
			case true:

				RegistryKey regMenu = null;
				RegistryKey regCmd  = null;

				string fullPath = ExeLocation;

				try {
					regMenu = Registry.CurrentUser.CreateSubKey(R1.Reg_Shell);
					regMenu?.SetValue(String.Empty, R1.Name);
					regMenu?.SetValue("Icon", $"\"{fullPath}\"");

					regCmd = Registry.CurrentUser.CreateSubKey(R1.Reg_Shell_Cmd);

					regCmd?.SetValue(String.Empty,
					                 $"\"{fullPath}\" -i \"%1\" -auto -s");
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
					Trace.WriteLine($"{ex.Message}");

					return false;
				}

				break;

		}

		return false;

	}

	internal static readonly string MyPicturesFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

	private static readonly string LinuxDesktopFile = Path.Combine(R1.Linux_Applications_Dir, R1.Linux_Desktop_File);

	/*
	 * HKEY_CLASSES_ROOT is an alias, a merging, of two other locations:
	 *		HKEY_CURRENT_USER\Software\Classes
	 *		HKEY_LOCAL_MACHINE\Software\Classes
	 */

	public static async Task<GHRelease[]> GetRepoReleasesAsync()
	{
		var res = await "https://api.github.com/repos/Decimation/SmartImage/releases"
			          .WithAutoRedirect(true)
			          .AllowAnyHttpStatus()
			          .WithHeaders(new
			          {
				          User_Agent = HttpUtilities.UserAgent
			          })
			          .OnError(e =>
			          {
				          e.ExceptionHandled = true;
			          })
			          .GetJsonAsync<GHRelease[]>();

		return res;
	}

	public static async Task<GHRelease> GetLatestReleaseAsync()
	{
		var r = await GetRepoReleasesAsync();

		if (r == null) {
			return null;
		}

		foreach (var x in r) {
			if (Version.TryParse(x.tag_name[1..], out var xv)) {
				x.Version = xv;
			}

		}

		return r.OrderByDescending(x => x.published_at).First();
	}
}

[USI(ImplicitUseTargetFlags.WithMembers)]
public class GHReleaseAsset
{

	public string url { get; set; }

	public int id { get; set; }

	public string node_id { get; set; }

	public string name { get; set; }

	public object label { get; set; }

	public GHUploader uploader { get; set; }

	public string content_type { get; set; }

	public string state { get; set; }

	public int size { get; set; }

	public int download_count { get; set; }

	public DateTime created_at { get; set; }

	public DateTime updated_at { get; set; }

	public string browser_download_url { get; set; }

}

[USI(ImplicitUseTargetFlags.WithMembers)]
public class GHAuthor
{

	public string login { get; set; }

	public int id { get; set; }

	public string node_id { get; set; }

	public string avatar_url { get; set; }

	public string gravatar_id { get; set; }

	public string url { get; set; }

	public string html_url { get; set; }

	public string followers_url { get; set; }

	public string following_url { get; set; }

	public string gists_url { get; set; }

	public string starred_url { get; set; }

	public string subscriptions_url { get; set; }

	public string organizations_url { get; set; }

	public string repos_url { get; set; }

	public string events_url { get; set; }

	public string received_events_url { get; set; }

	public string type { get; set; }

	public bool site_admin { get; set; }

}

[USI(ImplicitUseTargetFlags.WithMembers)]
public class GHReactions
{

	public string url { get; set; }

	public int total_count { get; set; }

	[JsonPropertyName("+1")]
	public int Plus1 { get; set; }

	[JsonPropertyName("-1")]
	public int Minus1 { get; set; }

	public int laugh { get; set; }

	public int hooray { get; set; }

	public int confused { get; set; }

	public int heart { get; set; }

	public int rocket { get; set; }

	public int eyes { get; set; }

}

[USI(ImplicitUseTargetFlags.WithMembers)]
public class GHUploader
{

	public string login { get; set; }

	public int id { get; set; }

	public string node_id { get; set; }

	public string avatar_url { get; set; }

	public string gravatar_id { get; set; }

	public string url { get; set; }

	public string html_url { get; set; }

	public string followers_url { get; set; }

	public string following_url { get; set; }

	public string gists_url { get; set; }

	public string starred_url { get; set; }

	public string subscriptions_url { get; set; }

	public string organizations_url { get; set; }

	public string repos_url { get; set; }

	public string events_url { get; set; }

	public string received_events_url { get; set; }

	public string type { get; set; }

	public bool site_admin { get; set; }

}

[USI(ImplicitUseTargetFlags.WithMembers)]
public class GHRelease
{

	public string url { get; set; }

	public string assets_url { get; set; }

	public string upload_url { get; set; }

	public string html_url { get; set; }

	public int id { get; set; }

	public GHAuthor author { get; set; }

	public string node_id { get; set; }

	public string tag_name { get; set; }

	public string target_commitish { get; set; }

	public string name { get; set; }

	public bool draft { get; set; }

	public bool prerelease { get; set; }

	public DateTime created_at { get; set; }

	public DateTime published_at { get; set; }

	public List<GHReleaseAsset> assets { get; set; }

	public string tarball_url { get; set; }

	public string zipball_url { get; set; }

	public string body { get; set; }

	public string discussion_url { get; set; }

	public GHReactions reactions { get; set; }

	[JsonIgnore]
	[NonSerialized]
	public Version Version;

}