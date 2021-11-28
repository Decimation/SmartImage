using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Kantan.Model;
using Kantan.Numeric;
using Kantan.Text;
using SmartImage.Lib.Utilities;
// ReSharper disable SuggestVarOrType_DeconstructionDeclarations

// ReSharper disable CognitiveComplexity
#pragma warning disable 8629,CA1416
#nullable disable

namespace SmartImage.Lib.Searching;

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
public sealed class ImageResult : IOutline, IDisposable
{
	/// <summary>
	/// Result url
	/// </summary>
	[MaybeNull]
	public Uri Url { get; set; }

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
	[CanBeNull]
	public string Description { get; set; }

	/// <summary>
	/// Artist, author, creator
	/// </summary>
	[CanBeNull]
	public string Artist { get; set; }

	/// <summary>
	/// Source
	/// </summary>
	[CanBeNull]
	public string Source { get; set; }

	/// <summary>
	/// Character(s) present in image
	/// </summary>
	[CanBeNull]
	public string Characters { get; set; }

	/// <summary>
	/// Site name
	/// </summary>
	[CanBeNull]
	public string Site { get; set; }

	/// <summary>
	/// Date of image
	/// </summary>
	public DateTime? Date { get; set; }

	/// <summary>
	///     Result name
	/// </summary>
	[CanBeNull]
	public string Name { get; set; }

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

	[CanBeNull]
	public Image Image { get; set; }

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
		Direct.Url      = result.Direct.Url;
		Similarity  = result.Similarity;
		Width       = result.Width;
		Height      = result.Height;
		Source      = result.Source;
		Characters  = result.Characters;
		Artist      = result.Artist;
		Site        = result.Site;
		Description = result.Description;
		Date        = result.Date;

		ReloadImageData();

	}

	public DirectImage Direct { get; internal set; } = new();

	public async Task<bool> TryScanForDirectImages()
	{
		if (Url == null || Direct.Url != null) {
			return true;
		}

		try {

			var directImages = await ImageHelper.ScanForImages(Url.ToString());

			Debug.WriteLine($"{nameof(ImageResult)}: Found {directImages.Count} direct images");

			var direct = directImages.FirstOrDefault();

			if (direct != null) {
				Direct    = direct;
				ReloadImageData();
				return true;
			}
		}
		catch {
			//
		}

		return false;
	}


	public bool IsAlreadyDirect()
	{
		if (Url is not { }) {
			return false;
		}

		var s = Url.ToString();

		var b = ImageHelper.IsImage(s, out var di);

		if (b) {

			try {
				Image = Image.FromStream(di.Stream);
			}
			catch (Exception e) {
			}

			Direct.Url = Url;
		}

		return b;
	}

	public void ReloadImageData()
	{
		if (Image is { }) {
			Width  = Image.Width;
			Height = Image.Height;
		}
	}

	public override string ToString() => Strings.OutlineString(this);

	public Dictionary<string, object> Outline
	{
		get
		{
			var map = new Dictionary<string, object>
			{
				{ nameof(Url), Url },
				{ "Direct Url",Direct.Url }
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
			return map;
		}
	}

	public void Dispose()
	{
		Image?.Dispose();
		Direct?.Dispose();
	}
}