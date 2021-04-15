using System;
using System.Collections.Generic;
using System.Text;
using SimpleCore.Utilities;

#nullable enable
namespace SmartImage.Lib.Searching
{
	/// <summary>
	/// Describes an image search result
	/// </summary>
	public sealed class ImageResult
	{
		/// <summary>
		/// Url
		/// </summary>
		public Uri? Url { get; set; }

		/// <summary>
		/// Similarity
		/// </summary>
		public float? Similarity { get; set; }

		/// <summary>
		/// Width
		/// </summary>
		public int? Width { get; set; }

		/// <summary>
		/// Height
		/// </summary>
		public int? Height { get; set; }

		/// <summary>
		/// Description, caption
		/// </summary>
		public string? Description { get; set; }

		/// <summary>
		/// Artist, author, creator
		/// </summary>
		public string? Artist { get; set; }

		/// <summary>
		/// Source
		/// </summary>
		public string? Source { get; set; }

		/// <summary>
		/// Character(s) present in image
		/// </summary>
		public string? Characters { get; set; }

		/// <summary>
		/// Site name
		/// </summary>
		public string? Site { get; set; }


		/// <summary>
		/// Date of image
		/// </summary>
		public DateTime? Date { get; set; }


		/// <summary>
		///     Result name
		/// </summary>
		public string? Name { get; set; }

		public int? Resolution => Width.HasValue && Height.HasValue ? Width * Height : -1;

		public Dictionary<string, object> OtherMetadata { get; }

		public ImageResult()
		{
			OtherMetadata = new();
		}

		public void UpdateFrom(ImageResult result)
		{
			Url         = result.Url;
			Similarity  = result.Similarity;
			Width       = result.Width;
			Height      = result.Height;
			Source      = result.Source;
			Characters  = result.Characters;
			Artist      = result.Artist;
			Site        = result.Site;
			Description = result.Description;
			Date        = result.Date;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();


			sb.Append($"{nameof(Url)}: {Url}\n");

			if (Similarity.HasValue) {
				sb.Append($"{nameof(Similarity)}: {Similarity.Value/100:P}\n");
			}

			if (Width.HasValue && Height.HasValue) {
				sb.Append($"Resolution: {Width}x{Height}\n");

			}

			if (Description != null) {
				sb.Append($"{nameof(Description)}: {Description}\n");

			}

			if (Artist != null) {
				sb.Append($"{nameof(Artist)}: {Artist}\n");

			}

			if (Site != null) {
				sb.Append($"{nameof(Site)}: {Site}\n");
			}

			
			
			return sb.ToString().RemoveLastOccurrence("\n");
		}
	}
}