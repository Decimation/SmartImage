using SimpleCore.Numeric;
using SimpleCore.Utilities;
using SmartImage.Lib.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

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

		public bool HasResolution => Width.HasValue && Height.HasValue;

		public int? Resolution => HasResolution ? Width * Height : -1;

		public float? Megapixels
		{
			get
			{
				if (HasResolution) {
					var mpx = (float) MathHelper.ConvertToUnit(Width!.Value * Height!.Value, MetricUnit.Mega);

					return mpx;
				}

				return null;
			}
		}

		public Dictionary<string, object> OtherMetadata { get; }

		public ImageResult()
		{
			OtherMetadata = new();
		}

		public int DetailScore
		{
			get
			{
				//todo: WIP

				int s = 0;

				var fields = GetType().GetRuntimeFields().Where(f => !f.IsStatic);

				foreach (FieldInfo f in fields) {
					var v = f.GetValue(this);

					if (v != null) {
						s++;
					}
				}

				s += OtherMetadata.Count - 1;

				return s;
			}
		}

		public ResolutionType ResolutionType
		{
			get
			{
				if (HasResolution) {
					var resolutionType = ImageUtilities.GetResolutionType(Width!.Value, Height!.Value);

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
			var sb = new StringBuilder();

			sb.AppendSafe(nameof(Url), Url);

			if (Similarity.HasValue) {
				sb.Append($"{nameof(Similarity)}: {Similarity.Value / 100:P}\n");
			}

			if (HasResolution) {
				sb.Append($"Resolution: {Width}x{Height} ({Megapixels:F} MP)");

				var resType = ResolutionType;

				if (resType != ResolutionType.Unknown) {
					sb.Append($" (~{resType})");
				}

				sb.Append("\n");
			}

			sb.AppendSafe(nameof(Name), Name);
			sb.AppendSafe(nameof(Description), Description);
			sb.AppendSafe(nameof(Artist), Artist);
			sb.AppendSafe(nameof(Site), Site);
			sb.AppendSafe(nameof(Source), Source);
			sb.AppendSafe(nameof(Characters), Characters);

			foreach (var (key, value) in OtherMetadata) {
				sb.AppendSafe(key, value);
			}

			sb.Append($"Detail score: {DetailScore}\n");

			return sb.ToString().RemoveLastOccurrence("\n");
		}
	}
}