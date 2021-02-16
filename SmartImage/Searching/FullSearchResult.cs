#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Novus.Win32;
using SimpleCore.Cli;
using SimpleCore.Net;
using SimpleCore.Numeric;
using SimpleCore.Utilities;
using SmartImage.Configuration;
using SmartImage.Core;
using SmartImage.Engines;
using SmartImage.Utilities;

#nullable disable
#pragma warning disable CS8632

namespace SmartImage.Searching
{
	/// <summary>
	///     Represents a complete search result
	/// </summary>
	/// <seealso cref="BaseSearchResult"/>
	public sealed class FullSearchResult : BaseSearchResult, NConsoleOption, IComparable<FullSearchResult>
	{
		private const string ORIGINAL_IMAGE_NAME = "(Original image)";

		public FullSearchResult(BaseSearchEngine engine, string url, float? similarity = null)
			: this(engine, engine.Color, engine.Name, url, similarity) { }

		/// <summary>
		/// Root constructor
		/// </summary>
		public FullSearchResult(BaseSearchEngine src, Color color, string name, string url, float? similarity = null)
		{
			SearchEngine = src;
			Url          = url;
			Name         = name;
			Color        = color;

			Similarity      = similarity;
			Metadata        = new Dictionary<string, object>();
			ExtendedResults = new List<FullSearchResult>();
		}


		/// <summary>
		/// Special constructor for original image
		/// </summary>
		private FullSearchResult(Color color, string name, string url, float? similarity = null)
			: this(null!, color, name, url, similarity) { }

		#region Properties

		/// <summary>
		///     Search engine
		/// </summary>
		public BaseSearchEngine SearchEngine { get; }

		/// <summary>
		///     Whether this result is a result from a priority engine (<see cref="SearchConfig.PriorityEngines" />)
		/// </summary>
		public bool IsPriority => !IsOriginal && SearchConfig.Config.PriorityEngines.HasFlag(SearchEngine.Engine) &&
		                          SearchEngine.Engine != SearchEngineOptions.None;


		/// <summary>
		///     Displays <see cref="ExtendedResults" />, if any, in a new menu
		/// </summary>
		public NConsoleFunction? AltFunction
		{
			get
			{
				return () =>
				{
					if (!ExtendedResults.Any()) {
						return null;
					}

					NConsole.ReadOptions(ExtendedResults);

					return null;
				};
			}
			set { }
		}

		public Color Color { get; set; }

		/// <summary>
		///     Downloads result (<see cref="Url" />) and opens it in Explorer with the file highlighted.
		/// </summary>
		/// <remarks>(Ideally, <see cref="Url" /> is a direct image link)</remarks>
		public NConsoleFunction CtrlFunction
		{
			get
			{
				return () =>
				{
					Debug.WriteLine("Downloading");

					try {
						NConsole.WriteSuccess("Downloading...");


						string? path = Network.DownloadUrl(Url);

						NConsole.WriteSuccess("Downloaded to {0}", path);

						// Open folder with downloaded file selected
						FileSystem.ExploreFile(path);


					}
					catch (Exception e) {
						NConsole.WriteError($"Error downloading: {e.Message}");
					}
					finally {
						NConsole.WaitForSecond();
					}


					return null;
				};
			}
			set { }
		}


		public string Data
		{
			get => ToString();
			set { }
		}

		/// <summary>
		///     Additional information about the image, results, and other related metadata
		/// </summary>
		public Dictionary<string, object> Metadata { get; }

		/// <summary>
		///     Direct source matches and other extended results
		/// </summary>
		/// <remarks>This list is used if there are multiple results</remarks>
		public List<FullSearchResult> ExtendedResults { get; }

		/// <summary>
		///     Opens <see cref="BaseSearchResult.Url" /> in browser
		/// </summary>
		public NConsoleFunction Function
		{
			get
			{
				return () =>
				{
					if (Url is null) {
						NConsole.WriteError("Result does not contain a URL");
						NConsole.WaitForSecond();
					}
					else {
						// Open in browser
						Network.OpenUrl(Url);
					}


					return null;
				};
			}
			set { }
		}

		/// <summary>
		///     Opens <see cref="RawUrl" /> in browser, if available
		/// </summary>
		public NConsoleFunction ComboFunction
		{
			get
			{
				return () =>
				{
					if (RawUrl != null) {
						Network.OpenUrl(RawUrl);
						return null;
					}

					NConsole.WriteError("Raw result unavailable");
					NConsole.WaitForSecond();
					return null;
				};
			}
			set { }
		}


		/// <summary>
		///     Result name
		/// </summary>
		public string Name { get; set; }


		/// <summary>
		///     Raw, undifferentiated search url
		/// </summary>
		public string? RawUrl { get; set; }

		/// <summary>
		/// Whether this is the original image
		/// </summary>
		public bool IsOriginal { get; set; }

		[CanBeNull]

		public string AspectRatio
		{
			get
			{
				if (HasResolution) {


					// ReSharper disable PossibleInvalidOperationException

					var fraction = new Fraction(Width.Value, Height.Value);

					const char FRAC  = '/';
					const char COLON = ':';

					var fractionStr = fraction.ToString();


					if (fractionStr.Length == 1) {
						fractionStr = fractionStr + COLON + fractionStr;
					}

					string? aspectRatio = fractionStr.Replace(FRAC, COLON);


					return aspectRatio;

					// ReSharper restore PossibleInvalidOperationException

				}

				return null;
			}
		}


		public bool HasResolution => Width.HasValue && Height.HasValue;

		public float? PixelResolution
		{
			get
			{
				if (HasResolution) {
					// ReSharper disable PossibleInvalidOperationException

					float mpx = (float) MathHelper.ConvertToUnit(Width.Value * Height.Value, MetricUnit.Mega);


					return mpx;

					// ReSharper restore PossibleInvalidOperationException
				}

				return null;
			}
		}

		#endregion

		public void AddErrorMessage(string msg)
		{
			Metadata.Add($"Error message", msg);
		}

		public void AddExtendedResults(BaseSearchResult[] bestImages)
		{
			ExtendedResults.AddRange(CreateExtendedResults(bestImages));
		}

		public int CompareTo(FullSearchResult? y)
		{
			float xSim = Similarity    ?? 0;
			float ySim = y?.Similarity ?? 0;

			if (xSim > ySim) {
				return -1;
			}

			if (xSim < ySim) {
				return 1;
			}

			if (ExtendedResults.Count > y?.ExtendedResults.Count) {
				return -1;
			}

			if (Metadata.Count > y?.Metadata.Count) {
				return -1;
			}

			return 0;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			/*
			 * Result symbols
			 */

			if (ExtendedResults.Any()) {
				sb.Append($"({ExtendedResults.Count})").Append(Formatting.SPACE);
			}

			if (Filter) {
				const string FILTER = "-";
				sb.Append(FILTER).Append(Formatting.SPACE);
			}

			if (IsPriority) {
				const string PRIORITY = "*";
				sb.Append(PRIORITY).Append(Formatting.SPACE);
			}

			sb.AppendLine();

			/*
			 * Result details
			 */

			AppendResultInfo(sb, "Result", Url, RawUrl != Url);
			AppendResultInfo(sb, "Raw", RawUrl);

			AppendResultInfo(sb, nameof(Similarity), $"{Similarity / 100:P}", Similarity.HasValue && !IsOriginal);

			AppendResultInfo(sb, "Resolution",
				$"{Width}x{Height} ({AspectRatio}) ({PixelResolution:F} MP)", HasResolution);

			AppendResultInfo(sb, nameof(Artist), Artist);
			AppendResultInfo(sb, nameof(Characters), Characters);
			AppendResultInfo(sb, nameof(Source), Source);
			AppendResultInfo(sb, nameof(Description), Description);
			AppendResultInfo(sb, nameof(Site), Site);
			AppendResultInfo(sb, nameof(Date), Date.ToString());


			foreach (var (key, value) in Metadata) {
				AppendResultInfo(sb, key, value.ToString());
			}


			return sb.ToString();
		}


		private static readonly List<Color> SimilarityColorGradient =
			ColorUtilities.GetGradients(ColorUtilities.AbsoluteRed, ColorUtilities.AbsoluteGreen, (int) MAX_SIMILARITY)
				.ToList();


		private void AppendResultInfo(StringBuilder sb, string name, string? value, bool cond = true)
		{
			if (cond && !String.IsNullOrWhiteSpace(value)) {

				var newColor = Interface.ColorMain3;

				if (name == nameof(Similarity) && Similarity.HasValue) {
					newColor = SimilarityColorGradient[(int) Similarity];
				}

				string? valueStr = value.AddColor(newColor);
				sb.Append($"\t{Formatting.ANSI_RESET}{name}: {valueStr}{Formatting.ANSI_RESET}\n");

			}
		}


		public void UpdateFrom(BaseSearchResult result)
		{
			Url         = result.Url;
			Similarity  = result.Similarity;
			Width       = result.Width;
			Height      = result.Height;
			Filter      = result.Filter;
			Source      = result.Source;
			Characters  = result.Characters;
			Artist      = result.Artist;
			Site        = result.Site;
			Description = result.Description;
			Date        = result.Date;
		}

		private FullSearchResult CreateExtendedResult(BaseSearchResult result)
		{
			var extendedResult = new FullSearchResult(SearchEngine, Color, Name, result.Url, result.Similarity);

			extendedResult.UpdateFrom(result);

			return extendedResult;
		}

		private IEnumerable<FullSearchResult> CreateExtendedResults(IReadOnlyList<BaseSearchResult> results)
		{
			var rg = new FullSearchResult[results.Count];

			for (int i = 0; i < rg.Length; i++) {

				var    result = results[i];
				string name   = $"Extended result #{i}";

				// Copy

				var extendedResult = CreateExtendedResult(result);
				extendedResult.Name = name;

				rg[i] = extendedResult;
			}


			return rg;
		}


		private const float MAX_SIMILARITY = 100.0f;

		/// <summary>
		///     Creates a <see cref="FullSearchResult" /> for the original image
		/// </summary>
		public static FullSearchResult GetOriginalImageResult(ImageInputInfo info)
		{
			using var bmp = (Bitmap) Image.FromStream(info.Stream);

			var result = new FullSearchResult(Interface.ColorMain2, ORIGINAL_IMAGE_NAME, info.ImageUrl)
			{
				IsOriginal = true,
				Similarity = MAX_SIMILARITY,
				Width      = bmp.Width,
				Height     = bmp.Height

			};

			/*
			 *
			 */

			string         name;
			FileFormatType fileFormat;
			double         bytes;

			if (info.IsUrl) {
				name = info.Value.ToString();


				info.Stream.Position = 0;
				using var ms = new MemoryStream();
				info.Stream.CopyTo(ms);
				var rg = ms.ToArray();
				fileFormat = FileSystem.ResolveFileType(rg);
				bytes      = rg.Length;
			}
			else if (info.IsFile) {
				var imageFile = (FileInfo) info.Value;

				fileFormat = FileSystem.ResolveFileType(imageFile.FullName);

				name  = imageFile.Name;
				bytes = FileSystem.GetFileSize(imageFile.FullName);


			}
			else {
				throw new SmartImageException();
			}

			string imgSize = MathHelper.ConvertToUnit(bytes);


			string imageInfoStr = $"{name} ({imgSize})";

			string infoStr = $"({fileFormat.Name})";

			result.Metadata.Add("Info", imageInfoStr);
			result.Metadata.Add("Image", infoStr);

			return result;
		}


		public static BaseSearchResult[] FilterAndSelectBestImages(List<BaseSearchResult> rg)
		{
			var best = rg.OrderByDescending(i => i.FullResolution)
				.Cast<BaseSearchResult>()
				.ToArray();

			return best;
		}

		/// <summary>
		/// Handles result opening from priority engines and filtering
		/// </summary>
		public void HandlePriorityResult()
		{
			/*
			 * Filtering is disabled
			 * Open it anyway
			 */

			if (!SearchConfig.Config.FilterResults) {
				Function();
				return;
			}

			/*
			 * Filtering is enabled
			 * Determine if it passes the threshold
			 */

			if (!Filter) {
				// Open result
				Function();
			}
			else {
				Debug.WriteLine($"Filtering result {Name}");
			}
		}
	}
}