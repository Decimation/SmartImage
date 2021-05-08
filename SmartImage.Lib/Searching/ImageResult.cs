using SimpleCore.Numeric;
using SimpleCore.Utilities;
using SmartImage.Lib.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using Novus.Utilities;

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

		/// <summary>
		/// Whether <see cref="Width"/> and <see cref="Height"/> values are available
		/// </summary>
		public bool HasImageDimensions => Width.HasValue && Height.HasValue;

		/// <summary>
		/// Pixel resolution
		/// </summary>
		public int? PixelResolution => HasImageDimensions ? Width * Height : null;


		/// <summary>
		/// <see cref="PixelResolution"/> expressed in megapixels.
		/// </summary>
		public float? MegapixelResolution
		{
			get
			{
				var px = PixelResolution;

				if (px.HasValue) {
					var mpx = (float) MathHelper.ConvertToUnit(px.Value, MetricUnit.Mega);

					return mpx;
				}

				return null;
			}
		}

		/// <summary>
		/// Other metadata about this image
		/// </summary>
		public Dictionary<string, object> OtherMetadata { get; }

		public ImageResult()
		{
			OtherMetadata = new();
		}


		private static List<FieldInfo> GetDetailFields()
		{
			var fields = typeof(ImageResult).GetRuntimeFields().Where(f => !f.IsStatic).ToList();

			fields.RemoveAll(f => f.Name.Contains(nameof(OtherMetadata)));

			return fields;
		}

		private static readonly List<FieldInfo> DetailFields = GetDetailFields();

		public int DetailScore
		{
			get
			{
				//todo: WIP

				/*
				 * The number of non-null fields
				 */

				int s = 0;


				foreach (FieldInfo f in DetailFields) {
					var v = f.GetValue(this);

					if (v != null) {
						s++;
					}
				}


				s += OtherMetadata.Count;

				return s;
			}
		}

		public bool IsDetailed => DetailScore >= (DetailFields.Count * .5);


		/// <summary>
		/// The display resolution of this image
		/// </summary>
		public DisplayResolutionType DisplayResolution
		{
			get
			{
				if (HasImageDimensions) {
					var resolutionType = ImageUtilities.GetDisplayResolution(Width!.Value, Height!.Value);

					return resolutionType;
				}

				throw new SmartImageException($"Resolution unavailable");
			}
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
			var sb = new ExtendedStringBuilder() {};

			sb.Append(nameof(Url), Url);

			if (Similarity.HasValue) {
				sb.Append($"{nameof(Similarity)}", $"{Similarity.Value / 100:P}");
			}

			if (HasImageDimensions) {
				string? val = $"{Width}x{Height} ({MegapixelResolution:F} MP)";


				var resType = DisplayResolution;

				if (resType != DisplayResolutionType.Unknown) {
					val += ($" (~{resType})");
				}

				sb.Append($"Resolution", val);

			}

			sb.Append(nameof(Name), Name);
			sb.Append(nameof(Description), Description);
			sb.Append(nameof(Artist), Artist);
			sb.Append(nameof(Site), Site);
			sb.Append(nameof(Source), Source);
			sb.Append(nameof(Characters), Characters);

			foreach (var (key, value) in OtherMetadata) {
				sb.Append(key, value);
			}

			sb.Append($"Detail score", $"{DetailScore}/{DetailFields.Count} ({(IsDetailed ? "Y" : "N")})");

			return sb.ToString().RemoveLastOccurrence("\n");
		}
	}
}