using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Kantan.Model;
using Kantan.Numeric;
using Kantan.Text;
using SmartImage.Lib.Utilities;

// ReSharper disable CognitiveComplexity
#pragma warning disable 8629
#nullable enable

namespace SmartImage.Lib.Searching
{
	public enum ResultQuality
	{
		NA,
		Low,
		High,
		Indeterminate,
	}

	/// <summary>
	/// Describes an image search result
	/// </summary>
	public sealed class ImageResult : IOutline
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
					float mpx = (float) MathHelper.ConvertToUnit(px.Value, MetricPrefix.Mega);

					return mpx;
				}

				return null;
			}
		}

		/// <summary>
		/// Other metadata about this image
		/// </summary>
		public Dictionary<string, object> OtherMetadata { get; }

		public Image? Image { get; set; }

		/// <summary>
		/// The display resolution of this image
		/// </summary>
		public DisplayResolutionType DisplayResolution
		{

			get
			{
				if (HasImageDimensions) {
					return ImageHelper.GetDisplayResolution(Width.Value, Height.Value);
				}

				throw new SmartImageException("Resolution unavailable");
			}

		}

		public ResultQuality Quality { get; set; }

		// TODO: Refactor detail score

		private static readonly List<FieldInfo> DetailFields = GetDetailFields();

		/// <summary>
		/// Score representing the number of fields that are populated (i.e., non-<c>null</c> or <c>default</c>);
		/// used as a heuristic for determining image result quality
		/// </summary>
		public int DetailScore
		{
			get
			{
				int s = DetailFields.Select(f => f.GetValue(this))
				                    .Count(v => v != null);

				s += OtherMetadata.Count;
				/*if (Similarity.HasValue) {
					s +=(int) Math.Ceiling(((Similarity.Value/100) * 13f) * .66f);
				}*/

				return s;
			}
		}

		public bool IsDetailed => DetailScore >= DetailFields.Count * .4;

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

			UpdateImageData();

		}

		public async void FindDirectImages()
		{
			if (Url == null || Direct != null)
				return;

			try {

				var directImages = await ImageHelper.FindDirectImages(Url.ToString());

				var direct = directImages.FirstOrDefault();

				if (direct != null) {
					Direct = new Uri((direct));
				}
			}
			catch {
				//
			}

			UpdateImageData();
		}

		public bool CheckDirect(DirectImageCriterion d)
		{
			if (Url is not { }) {
				return false;
			}

			var s = Url.ToString();

			var b = ImageHelper.IsImage(s, d);

			if (b) {
				Direct = Url;
			}

			return b;
		}

		public void UpdateImageData()
		{
			if (Image is { }) {

				Width  = Image.Width;
				Height = Image.Height;

				//
				// OtherMetadata.Add("Size", MathHelper.ConvertToUnit(rg.Length));
				// OtherMetadata.Add("Mime", MediaTypes.ResolveFromData(rg));
			}
		}

		public override string ToString() => Strings.OutlineString(this);

		public Dictionary<string, object> Outline
		{
			get
			{
#pragma warning disable CS8604

				var map = new Dictionary<string, object>
				{
					{ nameof(Url), Url },
					{ nameof(Direct), Direct }
				};

				if (Similarity.HasValue) {
					map.Add($"{nameof(Similarity)}", $"{Similarity.Value.AsPercent()}");
				}

				if (HasImageDimensions) {
					string val = $"{Width}x{Height} ({MegapixelResolution:F} MP)";

					var resType = DisplayResolution;

					if (resType != DisplayResolutionType.Unknown) {
						val += ($" (~{resType})");
					}

					map.Add("Resolution", val);
				}

				map.Add(nameof(Name), Name);

				if (Quality is not ResultQuality.NA) {
					map.Add(nameof(Quality), Quality);

				}

				map.Add(nameof(Description), Description);
				map.Add(nameof(Artist), Artist);
				map.Add(nameof(Site), Site);
				map.Add(nameof(Source), Source);
				map.Add(nameof(Characters), Characters);

				foreach (var (key, value) in OtherMetadata) {
					map.Add(key, value);
				}

				map.Add("Detail score", $"{DetailScore}/{DetailFields.Count} ({(IsDetailed ? "Y" : "N")})");

#pragma warning restore CS8604

				return map;
			}
		}
	}
}