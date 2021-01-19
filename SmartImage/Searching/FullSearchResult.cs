#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Novus.Utilities;
using Novus.Win32;
using SimpleCore.Cli;
using SimpleCore.Net;
using SimpleCore.Numeric;
using SimpleCore.Utilities;
using SmartImage.Core;
using SmartImage.Engines;
using SmartImage.Utilities;

namespace SmartImage.Searching
{
	/// <summary>
	///     Represents a complete search result
	/// </summary>
	public sealed class FullSearchResult : NConsoleOption, ISearchResult
	{
		public FullSearchResult(ISearchEngine engine, string url, float? similarity = null)
			: this(engine.Engine, engine.Color, engine.Name, url, similarity) { }

		public FullSearchResult(SearchEngineOptions src, Color color, string name, string url, float? similarity = null)
		{
			Engine = src;
			Url    = url;
			Name   = name;
			Color  = color;

			Similarity      = similarity;
			ExtendedInfo    = new List<string>();
			ExtendedResults = new List<FullSearchResult>();

		}

		/// <summary>
		/// Engine
		/// </summary>
		public SearchEngineOptions Engine { get; }

		/// <summary>
		/// Whether this result is a result from a priority engine (<see cref="SearchConfig.PriorityEngines"/>)
		/// </summary>
		public bool IsPriority => SearchConfig.Config.PriorityEngines.HasFlag(Engine);

		public bool IsAnalyzed { get; set; }

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
		///     Downloads result (<see cref="Url"/>) and opens it in Explorer with the file highlighted.
		/// 
		/// </summary>
		/// <remarks>(Ideally, <see cref="Url"/> is a direct image link)</remarks>
		public override NConsoleFunction CtrlFunction
		{
			get
			{
				return () =>
				{
					Debug.WriteLine("Downloading");

					try {
						NConsole.WriteSuccess("Downloading...");



						string? path  = Network.DownloadUrl(Url);

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
		///     Extended information about the image, results, and other related metadata
		/// </summary>
		public List<string> ExtendedInfo { get; }

		/// <summary>
		///     Direct source matches and other extended results
		/// </summary>
		/// <remarks>This list is used if there are multiple results</remarks>
		public List<FullSearchResult> ExtendedResults { get; }

		/// <summary>
		///     Opens <see cref="Url"/> in browser
		/// </summary>
		public override NConsoleFunction Function
		{
			get
			{
				return () =>
				{
					// Open in browser
					Network.OpenUrl(Url);
					return null;
				};
			}
		}

		/// <summary>
		/// Opens <see cref="RawUrl"/> in browser, if available
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

		public string? Description { get; set; }

		public int? Height { get; set; }

		public float? Similarity { get; set; }

		public string Url { get; set; }

		public int? Width { get; set; }

		public bool Filter { get; set; }

		public string? Artist { get; set; }

		public string? Source { get; set; }

		public string? Characters { get; set; }

		public string? SiteName { get; set; }

		public void AddExtendedResults(ISearchResult[] bestImages)
		{
			// todo?

			var rg = FromExtendedResult(bestImages);

			ExtendedResults.AddRange(rg);

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
				sb.Append("-").Append(Formatting.SPACE);
			}

			if (IsPriority) {
				sb.Append("*").Append(Formatting.SPACE);
			}

			sb.AppendLine();

			/*
			 * Result details
			 */

			if (RawUrl != Url) {
				sb.Append($"\tResult: {Url}\n");
			}

			AddInfoSafe(sb, RawUrl, "Raw");

			if (Similarity.HasValue) {
				sb.Append($"\tSimilarity: {Similarity / 100:P}\n");
			}

			AddInfoSafe(sb, Artist, nameof(Artist));
			AddInfoSafe(sb, Characters, nameof(Characters));
			AddInfoSafe(sb, Source, nameof(Source));
			AddInfoSafe(sb, Description, nameof(Description));
			AddInfoSafe(sb, SiteName, "Site");

			if (Width.HasValue && Height.HasValue) {
				sb.Append($"\tResolution: {Width}x{Height}\n");
			}

			foreach (string s in ExtendedInfo) {
				sb.Append($"\t{s}\n");
			}

			return sb.ToString();
		}

		private static void AddInfoSafe(StringBuilder sb, string? a, string n)
		{
			//todo: util

			if (!string.IsNullOrWhiteSpace(a)) {
				sb.Append($"\t{n}: {a}\n");
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
		}

		private FullSearchResult CreateExtendedResult(ISearchResult result)
		{
			var sr = new FullSearchResult(Engine, Color, Name, result.Url, result.Similarity)
			{
				Width       = result.Width,
				Height      = result.Height,
				Description = result.Description,
				Artist      = result.Artist,
				Source      = result.Source,
				Characters  = result.Characters,
				SiteName    = result.SiteName,
			};
			return sr;
		}

		private IEnumerable<FullSearchResult> FromExtendedResult(IReadOnlyList<ISearchResult> results)
		{
			var rg = new FullSearchResult[results.Count];

			for (int i = 0; i < rg.Length; i++) {
				var    result = results[i];
				string name   = $"Extended result #{i}";

				// Copy


				var sr = CreateExtendedResult(result);
				sr.Name = name;

				rg[i] = sr;
			}


			return rg;
		}

		public static int CompareResults(FullSearchResult x, FullSearchResult y)
		{
			float xSim = x?.Similarity ?? 0;
			float ySim = y?.Similarity ?? 0;

			if (xSim > ySim) {
				return -1;
			}

			if (xSim < ySim) {
				return 1;
			}

			if (x?.ExtendedResults.Count > y?.ExtendedResults.Count) {
				return -1;
			}

			if (x?.ExtendedInfo.Count > y?.ExtendedInfo.Count) {
				return -1;
			}

			return 0;
		}

		private const string ORIGINAL_IMAGE_NAME = "(Original image)";

		/// <summary>
		/// Creates a <see cref="FullSearchResult"/> for the original image
		/// </summary>
		public static FullSearchResult GetOriginalImageResult(string imageUrl, FileInfo imageFile)
		{
			var result = new FullSearchResult(SearchEngineOptions.None, Color.White, ORIGINAL_IMAGE_NAME, imageUrl)
			{
				Similarity = 100.0f,
				IsAnalyzed = true,
			};

			var fileFormat = FileSystem.ResolveFileType(imageFile.FullName);

			double fileSizeMegabytes =
				MathHelper.ConvertToUnit(FileSystem.GetFileSize(imageFile.FullName), MetricUnit.Mega);

			(int width, int height) = Images.GetDimensions(imageFile.FullName);

			result.Width  = width;
			result.Height = height;

			double mpx = MathHelper.ConvertToUnit(width * height, MetricUnit.Mega);

			var aspectRatio = new Fraction(width, height).ToString().Replace('/', ':');

			string infoStr =
				$"Info: {imageFile.Name} ({aspectRatio}) ({mpx:F} MP) ({fileSizeMegabytes:F} MB) ({fileFormat.Name})";

			result.ExtendedInfo.Add(infoStr);

			return result;
		}
	}
}