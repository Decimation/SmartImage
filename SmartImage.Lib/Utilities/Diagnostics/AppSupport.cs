// Author: Deci | Project: SmartImage.Lib | Name: AppSupport.cs
// Date: 2024/06/06 @ 14:06:00

#nullable disable

global using USI = JetBrains.Annotations.UsedImplicitlyAttribute;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.Json.Serialization;
using Flurl.Http;
using JetBrains.Annotations;
using Kantan.Net.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Novus.OS;
using Novus.Win32;
using Novus.Win32.Structures.User32;
using SmartImage.Lib.Utilities.Integration;

// ReSharper disable InconsistentNaming

namespace SmartImage.Lib.Utilities.Diagnostics;

internal static class AppSupport
{

	internal const string SI_DIAG_ID_0001 = "SI0001";

	internal static readonly ILoggerFactory Factory =
		LoggerFactory.Create(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Debug));

	[ICBN]
	public static Task<GitHubRelease[]> GetRepoReleasesAsync()
	{
		return R1.Url_GitHubApi
			.WithAutoRedirect(true)
			.AllowAnyHttpStatus()
			.WithHeaders(new
			{
				User_Agent = HttpUtilities.UserAgent
			})
			.OnError(e => { e.ExceptionHandled = true; })
			.GetJsonAsync<GitHubRelease[]>();
	}

	[ICBN]
	public static async Task<GitHubRelease> GetLatestReleaseAsync()
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

	/*
	 * HKEY_CLASSES_ROOT is an alias, a merging, of two other locations:
	 *		HKEY_CURRENT_USER\Software\Classes
	 *		HKEY_LOCAL_MACHINE\Software\Classes
	 */

}

#region GitHub types

[USI(ImplicitUseTargetFlags.WithMembers)]
public class GitHubReleaseAsset
{

	public string url { get; set; }

	public int id { get; set; }

	public string node_id { get; set; }

	public string name { get; set; }

	public object label { get; set; }

	public GitHubUploader uploader { get; set; }

	public string content_type { get; set; }

	public string state { get; set; }

	public int size { get; set; }

	public int download_count { get; set; }

	public DateTime created_at { get; set; }

	public DateTime updated_at { get; set; }

	public string browser_download_url { get; set; }

}

[USI(ImplicitUseTargetFlags.WithMembers)]
public class GitHubAuthor
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
public class GitHubReactions
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
public class GitHubUploader
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
public class GitHubRelease
{

	[JI]
	[NonSerialized]
	public Version Version;

	public string url { get; set; }

	public string assets_url { get; set; }

	public string upload_url { get; set; }

	public string html_url { get; set; }

	public int id { get; set; }

	public GitHubAuthor author { get; set; }

	public string node_id { get; set; }

	public string tag_name { get; set; }

	public string target_commitish { get; set; }

	public string name { get; set; }

	public bool draft { get; set; }

	public bool prerelease { get; set; }

	public DateTime created_at { get; set; }

	public DateTime published_at { get; set; }

	public List<GitHubReleaseAsset> assets { get; set; }

	public string tarball_url { get; set; }

	public string zipball_url { get; set; }

	public string body { get; set; }

	public string discussion_url { get; set; }

	public GitHubReactions reactions { get; set; }

}

#endregion