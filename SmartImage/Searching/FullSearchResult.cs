#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
	/// <seealso cref="ISearchResult"/>
	/// <seealso cref="BasicSearchResult"/>
	public sealed class FullSearchResult : NConsoleOption, ISearchResult, IComparable<FullSearchResult>
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
		public override NConsoleFunction? AltFunction
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
		}

		public override Color Color { get; set; }

		/// <summary>
		///     Downloads result (<see cref="Url" />) and opens it in Explorer with the file highlighted.
		/// </summary>
		/// <remarks>(Ideally, <see cref="Url" /> is a direct image link)</remarks>
		public override NConsoleFunction CtrlFunction
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
		}


		public override string Data => ToString();

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
		///     Opens <see cref="Url" /> in browser
		/// </summary>
		public override NConsoleFunction Function
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
		}

		/// <summary>
		///     Opens <see cref="RawUrl" /> in browser, if available
		/// </summary>
		public override NConsoleFunction ComboFunction
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
		}


		/// <summary>
		///     Result name
		/// </summary>
		public override string Name { get; set; }


		/// <summary>
		///     Raw, undifferentiated search url
		/// </summary>
		public string? RawUrl { get; set; }

		/// <summary>
		/// Whether this is the original image
		/// </summary>
		public bool IsOriginal { get; set; }


		/// <inheritdoc cref="ISearchResult.Description" />
		public string? Description { get; set; }

		/// <inheritdoc cref="ISearchResult.Height" />
		public int? Height { get; set; }

		/// <inheritdoc cref="ISearchResult.Similarity" />
		public float? Similarity { get; set; }

		/// <inheritdoc cref="ISearchResult.Url" />
		public string Url { get; set; }

		/// <inheritdoc cref="ISearchResult.Width" />
		public int? Width { get; set; }

		/// <inheritdoc cref="ISearchResult.Filter" />
		public bool Filter { get; set; }

		/// <inheritdoc cref="ISearchResult.Artist" />
		public string? Artist { get; set; }

		/// <inheritdoc cref="ISearchResult.Source" />
		public string? Source { get; set; }

		/// <inheritdoc cref="ISearchResult.Characters" />
		public string? Characters { get; set; }

		/// <inheritdoc cref="ISearchResult.SiteName" />
		public string? SiteName { get; set; }

		public DateTime? Date { get; set; }

		public void AddErrorMessage(string msg)
		{
			Metadata.Add($"Error message", msg);
		}

		public void AddExtendedResults(ISearchResult[] bestImages)
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

			AppendResultInfo(sb, nameof(Artist), Artist);
			AppendResultInfo(sb, nameof(Characters), Characters);
			AppendResultInfo(sb, nameof(Source), Source);
			AppendResultInfo(sb, nameof(Description), Description);
			AppendResultInfo(sb, "Site", SiteName);
			AppendResultInfo(sb, nameof(Date), Date.ToString());

			AppendResultInfo(sb, "Resolution", $"{Width}x{Height}", Width.HasValue && Height.HasValue);

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

				var newColor = Color.FromArgb(255, 180, 180, 180);

				if (name == nameof(Similarity) && Similarity.HasValue) {
					newColor = SimilarityColorGradient[(int) Similarity];
				}

				string? valueStr = value.AddColor(newColor);
				sb.Append($"\t{Formatting.ANSI_RESET}{name}: {valueStr}{Formatting.ANSI_RESET}\n");

			}
		}


		public void UpdateFrom(ISearchResult result)
		{
			Url         = result.Url;
			Similarity  = result.Similarity;
			Width       = result.Width;
			Height      = result.Height;
			Filter      = result.Filter;
			Source      = result.Source;
			Characters  = result.Characters;
			Artist      = result.Artist;
			SiteName    = result.SiteName;
			Description = result.Description;
			Date        = result.Date;
		}

		private FullSearchResult CreateExtendedResult(ISearchResult result)
		{
			var extendedResult = new FullSearchResult(SearchEngine, Color, Name, result.Url, result.Similarity)
			{
				Width       = result.Width,
				Height      = result.Height,
				Description = result.Description,
				Artist      = result.Artist,
				Source      = result.Source,
				Characters  = result.Characters,
				SiteName    = result.SiteName
			};
			return extendedResult;
		}

		private IEnumerable<FullSearchResult> CreateExtendedResults(IReadOnlyList<ISearchResult> results)
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

		private void AddImageInfo(ImageInputInfo info)
		{
			//todo

			Bitmap         bmp;
			string         name;
			FileFormatType fileFormat;
			double         bytes;

			if (info.IsUrl) {
				name = info.Value.ToString();

				//using var netStream = Network.GetStreamFromUrl(info.ImageUrl);
				bmp = (Bitmap) Image.FromStream(info.Stream);

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

				bmp   = new Bitmap(imageFile.FullName);
				name  = imageFile.Name;
				bytes = FileSystem.GetFileSize(imageFile.FullName);
			}
			else {
				throw new SmartImageException();
			}

			string imgSize = MathHelper.ConvertToUnit(bytes);

			(int width, int height) = (bmp.Width, bmp.Height);

			Width  = width;
			Height = height;

			double mpx = MathHelper.ConvertToUnit(width * height, MetricUnit.Mega);


			var fraction    = new Fraction(width, height);
			var fractionStr = fraction.ToString();

			if (fractionStr == "1") {
				fractionStr = "1:1";
			}

			string? aspectRatio = fractionStr.Replace('/', ':');

			string imageInfoStr = $"{name} ({imgSize})";

			string infoStr = $"({aspectRatio}) ({mpx:F} MP) ({fileFormat.Name})";

			Metadata.Add("Info", imageInfoStr);
			Metadata.Add("Image", infoStr);
		}

		/// <summary>
		///     Creates a <see cref="FullSearchResult" /> for the original image
		/// </summary>
		public static FullSearchResult GetOriginalImageResult(ImageInputInfo info)
		{
			var result = new FullSearchResult(Interface.ColorMisc2, ORIGINAL_IMAGE_NAME, info.ImageUrl)
			{
				IsOriginal = true,
				Similarity = MAX_SIMILARITY,

			};

			result.AddImageInfo(info);

			return result;
		}

		private const int TAKE_N = 10;

		public static ISearchResult[] FilterAndSelectBestImages(List<BasicSearchResult> rg, int take = TAKE_N)
		{
			var best = rg.OrderByDescending(i => i.FullResolution)
				.Take(take)
				.Cast<ISearchResult>()
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