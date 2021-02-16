using System;

#nullable enable
namespace SmartImage.Searching
{
	/// <summary>
	/// Base search result
	/// </summary>
	public class BaseSearchResult
	{
		/// <summary>
		/// Url
		/// </summary>
		public virtual string Url { get; set; } //todo

		/// <summary>
		/// Similarity
		/// </summary>
		public virtual float? Similarity { get; set; }

		/// <summary>
		/// Width
		/// </summary>
		public virtual int? Width { get; set; }

		/// <summary>
		/// Height
		/// </summary>
		public virtual int? Height { get; set; }

		/// <summary>
		/// Description, caption
		/// </summary>
		public virtual string? Description { get; set; }

		/// <summary>
		/// Artist, author, creator
		/// </summary>
		public virtual string? Artist { get; set; }

		/// <summary>
		/// Source
		/// </summary>
		public virtual string? Source { get; set; }

		/// <summary>
		/// Character(s) present in image
		/// </summary>
		public virtual string? Characters { get; set; }

		/// <summary>
		/// Site name
		/// </summary>
		public virtual string? Site { get; set; }

		/// <summary>
		/// Whether or not to filter this result
		/// </summary>
		public virtual bool Filter { get; set; }

		/// <summary>
		/// Date of image
		/// </summary>
		public virtual DateTime? Date { get; set; }

		/// <summary>
		///     Full image resolution
		/// </summary>
		public virtual int? FullResolution => Width * Height;

		public BaseSearchResult()
		{

		}

		/*public BasicSearchResult(string url, int? width, int? height)
			: this(url, null, width, height, null, null, null) { }

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
			Site    = siteName;
			Filter      = filter;
			Date        = null;//todo
		}*/
	}
}