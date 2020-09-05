using System;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace SmartImage.Utilities
{
	public readonly struct ReleaseInfo
	{
		public ReleaseInfo(string tagName, string htmlUrl, string publishedAt)
		{
			TagName     = tagName;
			HtmlUrl     = htmlUrl;
			PublishedAt = DateTime.Parse(publishedAt); //todo: wrong time


			// todo
			// hacky

			const string buildRevision = ".0.0";

			//const string buildRevision = ".0";
			var          versionStr    = tagName.Replace("v", String.Empty) + buildRevision;
			var parse = Version.Parse(versionStr);


			Version = parse;
		}

		public string TagName { get; }

		public string HtmlUrl { get; }

		public DateTime PublishedAt { get; }

		public Version Version { get; }

		public static ReleaseInfo LatestRelease()
		{
			// todo
			var rc = new RestClient("https://api.github.com/");
			var re = new RestRequest("repos/Decimation/SmartImage/releases");
			var rs = rc.Execute(re);
			var ja = JArray.Parse(rs.Content);

			var first = ja[0];

			var tagName = first["tag_name"];
			var url     = first["html_url"];
			var publish = first["published_at"];

			var r = new ReleaseInfo(tagName.ToString(), url.ToString(), publish.ToString());
			return r;
		}

		public override string ToString()
		{
			return String.Format("{0} {1} {2}", TagName, Version, PublishedAt);
		}
	}
}