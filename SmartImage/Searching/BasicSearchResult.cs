using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable
namespace SmartImage.Searching
{
	public class BasicSearchResult : ISearchResult
	{
		public string Url { get; set; }

		public float? Similarity { get; set; }

		public int? Width { get; set; }

		public int? Height { get; set; }


		public string? Description { get; set; }

		public string? Artist { get; set; }


		public string? Source { get; set; }


		public string? Characters { get; set; }


		public string? SiteName { get; set; }


		public bool Filter { get; set; }

		/// <summary>
		///     Full image resolution.
		/// </summary>
		public int? FullResolution => Width * Height;

		public BasicSearchResult(string url, int? width, int? height)
			: this(url, null, width, height, url, null, null) { }

		public BasicSearchResult(string url, float? similarity, int? width, int? height,
			string? siteName, string? source, string? description)
			: this(url, similarity, width, height, description, null, source, null, siteName, false) { }

		public BasicSearchResult(string url, float? similarity, string? description, string? artist, string? source,
			string? characters, string? siteName)
			: this(url, similarity, null, null, description, artist, source, characters, siteName, false) { }

		public BasicSearchResult(string url, float? similarity, int? width, int? height, string? description,
			string? artist, string? source, string? characters, string? siteName, bool filter)
		{
			Url         = url;
			Similarity  = similarity;
			Width       = width;
			Height      = height;
			Description = description;
			Artist      = artist;
			Source      = source;
			Characters  = characters;
			SiteName    = siteName;
			Filter      = filter;
		}
	}
}