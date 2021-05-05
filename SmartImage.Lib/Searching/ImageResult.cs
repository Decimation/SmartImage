using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using SimpleCore.Numeric;
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
						Debug.WriteLine($"{f.Name} {s} [{v}]");
					}
				}

				s += OtherMetadata.Count - 1;

				return s;
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


			sb.Append($"{nameof(Url)}: {Url}\n");

			if (Similarity.HasValue) {
				sb.Append($"{nameof(Similarity)}: {Similarity.Value / 100:P}\n");
			}

			if (HasResolution) {
				sb.Append($"Resolution: {Width}x{Height} ({Megapixels:F} MP)\n");
			}


			Append(Name, nameof(Name));
			Append(Description, nameof(Description));
			Append(Artist, nameof(Artist));
			Append(Site, nameof(Site));
			Append(Source, nameof(Source));
			Append(Characters, nameof(Characters));


			foreach (var (key, value) in OtherMetadata) {
				Append(value, key);
			}

			sb.Append($"Detail score: {DetailScore}\n");


			void Append(object? o, string s)
			{
				if (o != null) {
					sb.Append($"{s}: {o}\n");
				}
			}

			return sb.ToString().RemoveLastOccurrence("\n");
		}
	}
}