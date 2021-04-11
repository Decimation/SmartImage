using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable
namespace SmartImage.Lib
{
	public class ImageResult
	{
		/// <summary>
		/// Url
		/// </summary>
		public string? Url { get; set; }

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
		/// Whether or not to filter this result
		/// </summary>
		public bool Filter { get; set; }

		/// <summary>
		/// Date of image
		/// </summary>
		public DateTime? Date { get; set; }


		/// <summary>
		///     Result name
		/// </summary>
		public string? Name { get; set; }
	}
}