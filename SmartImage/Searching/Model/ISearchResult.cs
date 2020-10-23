#nullable enable
namespace SmartImage.Searching.Model
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
		///     Full image resolution.
		/// </summary>
		public int? FullResolution => Width * Height;

		/// <summary>
		///     Image caption/name/title.
		/// </summary>
		public string? Caption { get; set; }
	}
}