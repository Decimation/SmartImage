using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Kantan.Cli.Controls;
using Kantan.Model;
using Kantan.Net;
using Kantan.Net.Content;
using Kantan.Numeric;
using Kantan.Text;
using Kantan.Utilities;
using SmartImage.Lib.Utilities;

// ReSharper disable SuggestVarOrType_DeconstructionDeclarations

// ReSharper disable CognitiveComplexity
#pragma warning disable IDE0079
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
public sealed class ImageResult : IResult
{
	/// <summary>
	/// Result url
	/// </summary>
	[MaybeNull]
	public Uri Url { get; set; }

	public List<Uri> OtherUrl { get; set; }

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

	/// <summary>
	/// The display resolution of this image
	/// </summary>
	public DisplayResolutionType DisplayResolution
	{
		get
		{
			// ReSharper disable PossibleInvalidOperationException

			if (HasImageDimensions) {
				return ImageManipulation.GetDisplayResolution(Width.Value, Height.Value);
			}

			throw new SmartImageException("Resolution unavailable");
			// ReSharper restore PossibleInvalidOperationException

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

	public SearchResult Root { get; internal set; }

	public ImageResult(SearchResult root)
	{
		Root          = root;
		OtherUrl      = new();
		OtherMetadata = new Dictionary<string, object>();
	}

	private static List<FieldInfo> GetDetailFields()
	{
		var fields = typeof(ImageResult).GetRuntimeFields()
		                                .Where(f => !f.IsStatic)
		                                .ToList();

		fields.RemoveAll(f => f.Name.Contains(nameof(OtherMetadata)));

		return fields;
	}

	public void UpdateFrom(ImageResult result)
	{
		Url          = result.Url;
		DirectImages = result.DirectImages;
		Similarity   = result.Similarity;
		Width        = result.Width;
		Height       = result.Height;
		Source       = result.Source;
		Characters   = result.Characters;
		Artist       = result.Artist;
		Site         = result.Site;
		Description  = result.Description;
		Date         = result.Date;
		OtherUrl     = result.OtherUrl;

	}

	public List<HttpResource> DirectImages { get; internal set; } = new() { };

	public HttpResource DirectImage
	{
		get => DirectImages.FirstOrDefault();
		set => DirectImages[0] = value;
	}

	public bool ScanForBinaryImages(int ms)
	{
		if (Url == null) {
			return false;
		}

		var url = Url.ToString();

		var di = HttpResource.GetAsync(url);
		di.Wait();

		var o = di.Result;
		o?.Resolve();
		var info = o;

		if (info == null) {
			return false;
		}

		if (info.IsBinary) {
			DirectImages.Add(info);
			return true;
		}
		else {
			info.Dispose();
		}

		if (DirectImages.Any()) {
			return true;

		}

		try {

			var async = HttpResourceFilter.Media.ScanAsync(url);
			async.Wait();

			var directImages = (async.Result.Where(x => x is { Url: { } })).ToArray();

			if (directImages.Any()) {
				// Debug.WriteLine($"{Url}: Found {directImages.Count} direct images");

				DirectImages.AddRange(directImages);

				return true;
			}
		}
		catch {
			//
		}

		return false;
	}

	public Dictionary<string, object> Data
	{
		get
		{
			var map = new Dictionary<string, object>
			{
				{ nameof(Url), Url },
				{ "Direct Url", DirectImage?.Url }
			};

			if (Similarity.HasValue) {
				map.Add($"{nameof(Similarity)}", $"{Similarity.Value / 100:P}");
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
		foreach (var t in DirectImages) {
			t.Dispose();
		}

		DirectImages.Clear();
	}

	public ConsoleOption GetConsoleOption()
	{
		var option = new ConsoleOption
		{
			Data = Data,
			Functions =
			{
				[ConsoleOption.NC_FN_MAIN]  = IResult.GetOpenFunction(Url),
				[ConsoleOption.NC_FN_COMBO] = IResult.GetDownloadFunction(() => new Uri(DirectImage.Url))
			}
		};

		return option;
	}

	public ConsoleOption GetConsoleOption(string n, Color c)
	{
		var option = GetConsoleOption();
		option.Color = c;
		option.Name  = n;

		return option;
	}
}