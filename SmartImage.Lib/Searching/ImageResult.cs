using SimpleCore.Numeric;
using SimpleCore.Utilities;
using SmartImage.Lib.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Novus.Win32;
using SimpleCore.Net;

#nullable enable

namespace SmartImage.Lib.Searching
{
	/// <summary>
	/// Describes an image search result
	/// </summary>
	public sealed class ImageResult
	{
		/// <summary>
		/// Result url
		/// </summary>
		public Uri? Url { get; set; }

		/// <summary>
		/// Direct image link of <see cref="Url"/>
		/// </summary>
		public Uri? Direct { get; set; }

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
				int? px = PixelResolution;

				if (px.HasValue) {
					float mpx = (float) MathHelper.ConvertToUnit(px.Value, MetricUnit.Mega);

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
			OtherMetadata = new Dictionary<string, object>();
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

				int s = DetailFields.Select(f => f.GetValue(this)).Count(v => v != null);

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
					var resolutionType = ImageHelper.GetDisplayResolution(Width!.Value, Height!.Value);

					return resolutionType;
				}

				throw new SmartImageException($"Resolution unavailable");
			}
		}

		public void UpdateFrom(ImageResult result)
		{
			Url         = result.Url;
			Direct      = result.Direct;
			Similarity  = result.Similarity;
			Width       = result.Width;
			Height      = result.Height;
			Source      = result.Source;
			Characters  = result.Characters;
			Artist      = result.Artist;
			Site        = result.Site;
			Description = result.Description;
			Date        = result.Date;

			if (result.Direct is { }) {
				var stream = WebUtilities.GetStream(result.Direct.ToString());
				var image  = Image.FromStream(stream);


				Width  = image.Width;
				Height = image.Height;

				OtherMetadata.Add("Mime", MediaTypes.ResolveFromData(stream));
			}


		}

		public async void FindDirectImagesAsync()
		{
			if (Url is not null) {
				var directImages = await ImageHelper.FindDirectImagesAsync(Url?.ToString());

				if (directImages is { }) {
					string? images = directImages.FirstOrDefault();

					if (images is { }) {
						var uri = new Uri(images);
						Direct = uri;
						Debug.WriteLine($"{Url} -> {Direct}");
					}
				}
			}
		}

		public string ToString(bool indent)
		{
			var sb = new ExtendedStringBuilder() { };

			sb.Append(nameof(Url), Url);
			sb.Append(nameof(Direct), Direct);

			if (Similarity.HasValue) {
				sb.Append($"{nameof(Similarity)}", $"{Similarity.Value / 100:P}");
			}

			if (HasImageDimensions) {
				string val = $"{Width}x{Height} ({MegapixelResolution:F} MP)";

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

			var xs = sb.ToString().RemoveLastOccurrence("\n");

			return indent ? Strings.Indent(xs) : xs;
		}

		public override string ToString() => ToString(false);
	}
}