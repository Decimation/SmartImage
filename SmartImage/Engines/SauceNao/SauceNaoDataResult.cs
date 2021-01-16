using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace SmartImage.Engines.SauceNao
{
	public class SauceNaoDataResult
	{
		/// <summary>
		///     The url(s) where the source is from. Multiple will be returned if the exact same image is found in multiple places
		/// </summary>
		public string[] Urls { get; internal set; }

		/// <summary>
		///     The search index of the image
		/// </summary>
		public SauceNaoSiteIndex Index { get; internal set; }

		/// <summary>
		///     How similar is the image to the one provided (Percentage)?
		/// </summary>
		public float Similarity { get; internal set; }

		public string WebsiteTitle { get; set; }

		public string Character { get; internal set; }

		public string Material { get; internal set; }

		public string Creator { get; internal set; }

		public override string ToString()
		{
			string firstUrl = Urls != null ? Urls[0] : "-";

			return $"{firstUrl} ({Similarity}, {Index})";
		}
	}
}