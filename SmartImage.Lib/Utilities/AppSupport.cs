// Author: Deci | Project: SmartImage.Lib | Name: AppSupport.cs
// Date: 2024/12/04 @ 22:12:34

#pragma warning disable IDE1006
using System.Reflection;
using System.Text.Json.Serialization;
using Flurl.Http;
using JetBrains.Annotations;
using Kantan.Net.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;

// ReSharper disable InconsistentNaming

namespace SmartImage.Lib.Utilities;

public static class AppSupport
{

	internal static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

	internal static readonly Version Version = Assembly.GetName().Version;

	internal static readonly ILoggerFactory Factory =
		LoggerFactory.Create(static builder =>
		{
			builder.AddDebug()
				// .AddProvider(new DebugLoggerProvider())
				.SetMinimumLevel(LogLevel.Trace);
		});

	public static async Task<GitHubRelease[]> GetRepoReleasesAsync()
	{
		var r = await R1.Url_GitHubApi
			        .WithAutoRedirect(true)
			        .AllowAnyHttpStatus()
			        .WithHeaders(new
			        {
				        User_Agent = R1.UserAgent1
			        })
			        .OnError(static e => { e.ExceptionHandled = true; })
			        .GetJsonAsync<GitHubRelease[]>();

		if (r == null) {
			return [];
		}

		foreach (var x in r) {
			var s = x.tag_name[1..].Split('-')[0];
			x.IsRdx = x.name.Contains("Rdx", StringComparison.CurrentCultureIgnoreCase);

			if (Version.TryParse(s, out var xv)) {
				x.Version = xv;
			}
		}

		return r;
	}

	/*
	 * HKEY_CLASSES_ROOT is an alias, a merging, of two other locations:
	 *		HKEY_CURRENT_USER\Software\Classes
	 *		HKEY_LOCAL_MACHINE\Software\Classes
	 */


	public const string DIAG_ID_EXPERIMENTAL = "SI_EXP_001";

}

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

	[JPN("+1")]
	public int Plus1 { get; set; }

	[JPN("-1")]
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
	[field: NonSerialized]
	public Version Version { get; set; }

	[JI]
	[field: NonSerialized]
	public bool IsRdx { get; set; }


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