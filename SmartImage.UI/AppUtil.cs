global using R2 = SmartImage.UI.Resources;
global using R1 = SmartImage.Lib.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using Novus.OS;
using Flurl.Http;
using JetBrains.Annotations;
using Kantan.Net.Utilities;
using Newtonsoft.Json;
// ReSharper disable InconsistentNaming

#nullable disable
namespace SmartImage.UI;

internal static class AppUtil
{
	#region

	public static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

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

	static AppUtil()
	{

	}

	public static Version Version => Assembly.GetName().Version;

	public static string CurrentAppFolder => Path.GetDirectoryName(ExeLocation);

	public static bool IsAppFolderInPath => FileSystem.IsFolderInPath(CurrentAppFolder);

	public static bool IsOnTop { get; private set; }

	#endregion

	public static bool IsContextMenuAdded
	{
		get
		{
			var reg = Registry.CurrentUser.OpenSubKey(R1.Reg_Shell_Cmd);
			return reg != null;
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
					                 $"\"{fullPath}\" -i \"%1\" -as");
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

	internal static async Task<GHRelease[]> GetRepoReleasesAsync()
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

		return r.OrderByDescending(x => x.published_at).FirstOrDefault(x =>
		{
			if (Version.TryParse(x.tag_name[1..], out var xv)) {
				x.Version = xv;
				return xv > Version;
			}

			return false;
		});
	}

	// Root myDeserializedClass = JsonConvert.DeserializeObject<List<Root>>(myJsonResponse);
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal class GHReleaseAsset
{
	public string     url                  { get; set; }
	public int        id                   { get; set; }
	public string     node_id              { get; set; }
	public string     name                 { get; set; }
	public object     label                { get; set; }
	public GHUploader uploader             { get; set; }
	public string     content_type         { get; set; }
	public string     state                { get; set; }
	public int        size                 { get; set; }
	public int        download_count       { get; set; }
	public DateTime   created_at           { get; set; }
	public DateTime   updated_at           { get; set; }
	public string     browser_download_url { get; set; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal class GHAuthor
{
	public string login               { get; set; }
	public int    id                  { get; set; }
	public string node_id             { get; set; }
	public string avatar_url          { get; set; }
	public string gravatar_id         { get; set; }
	public string url                 { get; set; }
	public string html_url            { get; set; }
	public string followers_url       { get; set; }
	public string following_url       { get; set; }
	public string gists_url           { get; set; }
	public string starred_url         { get; set; }
	public string subscriptions_url   { get; set; }
	public string organizations_url   { get; set; }
	public string repos_url           { get; set; }
	public string events_url          { get; set; }
	public string received_events_url { get; set; }
	public string type                { get; set; }
	public bool   site_admin          { get; set; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal class GHReactions
{
	public string url         { get; set; }
	public int    total_count { get; set; }

	[JsonProperty("+1")]
	public int Plus1 { get; set; }

	[JsonProperty("-1")]
	public int Minus1 { get; set; }

	public int laugh    { get; set; }
	public int hooray   { get; set; }
	public int confused { get; set; }
	public int heart    { get; set; }
	public int rocket   { get; set; }
	public int eyes     { get; set; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal class GHUploader
{
	public string login               { get; set; }
	public int    id                  { get; set; }
	public string node_id             { get; set; }
	public string avatar_url          { get; set; }
	public string gravatar_id         { get; set; }
	public string url                 { get; set; }
	public string html_url            { get; set; }
	public string followers_url       { get; set; }
	public string following_url       { get; set; }
	public string gists_url           { get; set; }
	public string starred_url         { get; set; }
	public string subscriptions_url   { get; set; }
	public string organizations_url   { get; set; }
	public string repos_url           { get; set; }
	public string events_url          { get; set; }
	public string received_events_url { get; set; }
	public string type                { get; set; }
	public bool   site_admin          { get; set; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal class GHRelease
{
	public string                     url              { get; set; }
	public string                     assets_url       { get; set; }
	public string                     upload_url       { get; set; }
	public string                     html_url         { get; set; }
	public int                        id               { get; set; }
	public GHAuthor             author           { get; set; }
	public string                     node_id          { get; set; }
	public string                     tag_name         { get; set; }
	public string                     target_commitish { get; set; }
	public string                     name             { get; set; }
	public bool                       draft            { get; set; }
	public bool                       prerelease       { get; set; }
	public DateTime                   created_at       { get; set; }
	public DateTime                   published_at     { get; set; }
	public List<GHReleaseAsset> assets           { get; set; }
	public string                     tarball_url      { get; set; }
	public string                     zipball_url      { get; set; }
	public string                     body             { get; set; }
	public string                     discussion_url   { get; set; }
	public GHReactions          reactions        { get; set; }

	[JsonIgnore]
	[NonSerialized]
	public Version Version;
}