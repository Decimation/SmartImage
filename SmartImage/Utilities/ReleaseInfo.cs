using System;
using System.Globalization;
using Newtonsoft.Json.Linq;
using RestSharp;
using Kantan.Diagnostics;

// ReSharper disable PossibleNullReferenceException

namespace SmartImage.Utilities
{
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
			var rc = new RestClient(GITHUB_API_ENDPOINT);
			var re = new RestRequest(GITHUB_API_SMARTIMAGE);
			var rs = rc.Execute(re);
			var ja = JArray.Parse(rs.Content);

			var first = ja[0];

			var tagName = first["tag_name"];
			var url     = first["html_url"];
			var publish = first["published_at"];

			var assets = first["assets"];
			var dlUrl  = assets[0]["browser_download_url"];

			var r = new ReleaseInfo(tagName.ToString(), url.ToString(), publish.ToString(), dlUrl.ToString());

			return r;
		}

		public override string ToString()
		{
			return $"{TagName} ({Version}) @ {PublishedAt}";
		}
	}
}