using System;
using System.Diagnostics;
using System.Globalization;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using Kantan.Diagnostics;
using Newtonsoft.Json;

// ReSharper disable PossibleNullReferenceException

namespace SmartImage.Utilities;

public readonly struct ReleaseInfo
{
	private const string GITHUB_API_ENDPOINT = "https://api.github.com/";

	private const string GITHUB_API_SMARTIMAGE = "repos/Decimation/SmartImage/releases";

	public ReleaseInfo(string tagName, string htmlUrl, string publishedAt, string asset)
	{
		// TODO: fails if tag contains non-numeric values!

		TagName = tagName;
		HtmlUrl = htmlUrl;

		var utc = DateTime.Parse(publishedAt, null, DateTimeStyles.AdjustToUniversal);

		PublishedAt = TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.Local);

		// Parse version

		string[] versionStrSplit = tagName.Replace("v", String.Empty)
		                                  .Split(".");

		int major = Int32.Parse(versionStrSplit[0]);
		int minor = Int32.Parse(versionStrSplit[1]);

		int build = 0, rev = 0;

		if (versionStrSplit.Length >= 3) {
			build = Int32.Parse(versionStrSplit[2]);
		}

		if (versionStrSplit.Length >= 4) {
			rev = Int32.Parse(versionStrSplit[3]);
		}

		Version = new Version(major, minor, build, rev);

		AssetUrl = asset;
	}

	public string TagName { get; }

	public string HtmlUrl { get; }

	public DateTime PublishedAt { get; }

	public Version Version { get; }

	public string AssetUrl { get; }

	public static ReleaseInfo GetLatestRelease()
	{
		const string s =
			"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.45 Safari/537.36";

		var task = (GITHUB_API_ENDPOINT + GITHUB_API_SMARTIMAGE)
		           .WithHeader("User-Agent", s)
		           .GetJsonListAsync();

		task.Wait();

		var list  = task.Result;
		var first = list[0];

		var tagName = first.tag_name;
		var url     = first.html_url;
		var publish = first.published_at;
		var assets  = first.assets;
		var dlUrl   = assets[0].browser_download_url;

		var r = new ReleaseInfo(tagName.ToString(), url.ToString(), publish.ToString(), dlUrl.ToString());

		return r;
	}

	public override string ToString()
	{
		return $"{TagName} ({Version}) @ {PublishedAt}";
	}
}