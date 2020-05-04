using System;

namespace SmartImage.Model
{
	public readonly struct ReleaseInfo
	{
		public ReleaseInfo(string tagName, string htmlUrl, string publishedAt)
		{
			TagName     = tagName;
			HtmlUrl     = htmlUrl;
			PublishedAt = DateTime.Parse(publishedAt); //todo: wrong time


			// hacky
			const string buildRevision = ".0.0";
			var          versionStr    = tagName.Replace("v", string.Empty) + buildRevision;

			var parse = System.Version.Parse(versionStr);


			Version = parse;
		}

		public string TagName { get; }

		public string HtmlUrl { get; }

		public DateTime PublishedAt { get; }

		public Version Version { get; }
	}
}