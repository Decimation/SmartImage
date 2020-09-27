using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
#nullable enable
namespace SmartImage.Searching
{
	public interface IExtendedSearchResult
	{
		/// <summary>
		/// Best match
		/// </summary>
		public string Url { get; }

		/// <summary>
		/// Image similarity
		/// </summary>
		public float? Similarity { get;  set; }

		public int? Width { get;  set; }

		public int? Height { get;  set; }


		public int? FullResolution => Width * Height;

		public string? Caption { get;  set; }
	}
}
