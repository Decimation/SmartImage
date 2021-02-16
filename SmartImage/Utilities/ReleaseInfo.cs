using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Globalization;

namespace SmartImage.Utilities
{
	public readonly struct ReleaseInfo
	{
		public ReleaseInfo(string tagName, string htmlUrl, string publishedAt, string asset)
		{
			TagName = tagName;
			HtmlUrl = htmlUrl;


			var utc = DateTime.Parse(publishedAt, null, DateTimeStyles.AdjustToUniversal);

			PublishedAt = TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.Local);


			// Parse version

			var versionStrSplit = tagName
				.Replace("v", String.Empty)
				.Split(".");


			int major = 0, minor = 0, build = 0, rev = 0;

			major = int.Parse(versionStrSplit[0]);
			minor = int.Parse(versionStrSplit[1]);

			if (versionStrSplit.Length >= 3)
			{
				build = int.Parse(versionStrSplit[2]);
			}

			if (versionStrSplit.Length >= 4)
			{
				rev = int.Parse(versionStrSplit[3]);
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
			// todo
			var rc = new RestClient("https://api.github.com/");
			var re = new RestRequest("repos/Decimation/SmartImage/releases");
			var rs = rc.Execute(re);
			var ja = JArray.Parse(rs.Content);

			var first = ja[0];

			var tagName = first["tag_name"];
			var url = first["html_url"];
			var publish = first["published_at"];


			var assets = first["assets"];
			var dlurl = assets[0]["browser_download_url"];

			var r = new ReleaseInfo(tagName.ToString(), url.ToString(), publish.ToString(), dlurl.ToString());


			return r;
		}

		public override string ToString()
		{
			return $"{TagName} ({Version}) @ {PublishedAt}";
		}
	}
}