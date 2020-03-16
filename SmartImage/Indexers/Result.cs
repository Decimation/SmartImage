using System.Runtime.Serialization;

namespace SmartImage.Indexers
{
	[DataContract]
	public class Result
	{
		/// <summary>
		/// The url(s) where the source is from. Multiple will be returned if the exact same image is found in multiple places
		/// </summary>
		[DataMember(Name = "ext_urls")]
		public string[] Url { get; internal set; }

		/// <summary>
		/// The search index of the image
		/// </summary>
		[DataMember(Name = "index_id")]
		public SiteIndex Index { get; internal set; }

		/// <summary>
		/// How similar is the image to the one provided (Percentage)?
		/// </summary>
		[DataMember(Name = "similarity")]
		public float Similarity { get; internal set; }

		/// <summary>
		/// A link to the thumbnail of the image
		/// </summary>
		[DataMember(Name = "thumbnail")]
		public string Thumbnail { get; internal set; }

		/// <summary>
		/// The name of the website it came from
		/// </summary>
		[IgnoreDataMember]
		public string WebsiteName { get; internal set; }

		/// <summary>
		/// How explicit is the image?
		/// </summary>
		[IgnoreDataMember]
		public float Rating { get; internal set; }


		[IgnoreDataMember]
		public string WebsiteTitle { get; set; }

		public override string ToString()
		{
			var firstUrl = Url != null ? Url[0] : "-";
			
			return string.Format("{0} ({1}, {2})", firstUrl, Similarity,Index);
		}
	}
}