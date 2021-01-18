using SmartImage.Core;

#nullable enable
namespace SmartImage.Searching
{
	/// <summary>
	///     Represents a search result
	/// </summary>
	public interface ISearchResult
	{
		/// <summary>
		///     Url of the best match found.
		/// </summary>
		public string Url { get; set; }

		/// <summary>
		///     Image similarity (delta).
		/// </summary>
		public float? Similarity { get; set; }

		/// <summary>
		///     Image width dimension.
		/// </summary>
		public int? Width { get; set; }

		/// <summary>
		///     Image height dimension.
		/// </summary>
		public int? Height { get; set; }



		/// <summary>
		///     Image description/caption/name/title.
		/// </summary>
		public string? Description { get; set; }

		/// <summary>
		/// Image artist
		/// </summary>
		public string? Artist { get; set; }

		/// <summary>
		/// Image source
		/// </summary>
		public string? Source { get; set; }

		/// <summary>
		/// Characters in the image
		/// </summary>
		public string? Characters { get; set; }

		/// <summary>
		/// Site name of <see cref="Url"/>
		/// </summary>
		public string? SiteName { get; set; }

		/// <summary>
		/// Filter this result if <see cref="SearchConfig.FilterResults"/> is used
		/// </summary>
		public bool Filter { get; set; }

	}
}