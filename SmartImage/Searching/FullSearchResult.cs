#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;
using Novus.Win32;
using SimpleCore.Console.CommandLine;
using SimpleCore.Net;
using SimpleCore.Utilities;
using SmartImage.Engines;

namespace SmartImage.Searching
{
	/// <summary>
	///     Contains search result and information
	/// </summary>
	public sealed class FullSearchResult : NConsoleOption, ISearchResult
	{
		public const char ATTR_SUCCESS = Formatting.CHECK_MARK;

		public FullSearchResult(ISearchEngine engine, string url, float? similarity = null)
			: this(engine.Color, engine.Name, url, similarity) { }

		public FullSearchResult(Color color, string name, string url, float? similarity = null)
		{
			Url   = url;
			Name  = name;
			Color = color;

			Similarity      = similarity;
			ExtendedInfo    = new List<string>();
			ExtendedResults = new List<FullSearchResult>();


		}

		/// <summary>
		///     Displays <see cref="ExtendedResults" /> if any
		/// </summary>
		public override NConsoleFunction? AltFunction { get; set; }

		public override Color Color { get; set; }

		/// <summary>
		///     Downloads image, if possible, and opens it in Explorer highlighted
		/// </summary>
		public override NConsoleFunction? CtrlFunction
		{
			get
			{
				return () =>
				{
					// if (!IsImage.HasValue || !IsImage.Value) {
					// 	bool ok = NConsoleIO.ReadConfirmation(
					// 		$"Link may not be an image. Download anyway?");
					// 	
					// 	if (!ok) {
					// 		return null;
					// 	}
					// }

					Debug.WriteLine("Downloading");

					string? path = Network.DownloadUrl(Url);

					NConsole.WriteSuccess("Downloaded to {0}", path);

					// Open folder with downloaded file selected
					FileSystem.ExploreFile(path);


					NConsoleIO.WaitForSecond();

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
		///     Opens result in browser
		/// </summary>
		public override NConsoleFunction Function
		{
			get
			{
				return () =>
				{
					Network.OpenUrl(Url);
					return null;
				};
			}
		}

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
					NConsoleIO.WaitForSecond();
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

		public string? Caption { get; set; }

		public int? Height { get; set; }

		public float? Similarity { get; set; }

		public string Url { get; set; }

		public int? Width { get; set; }

		public void AddExtendedResults(ISearchResult[] bestImages)
		{
			// todo?

			var rg = FromExtendedResult(bestImages);

			ExtendedResults.AddRange(rg);

			AltFunction = () =>
			{
				NConsoleIO.ReadOptions(ExtendedResults);

				return null;
			};
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			string attrSuccess = ATTR_SUCCESS.ToString();


			var ex = ExtendedResults.Count > 0
				? String.Format($"({ExtendedResults.Count})")
				: String.Empty;

			sb.Append($"{attrSuccess} {ex}\n");


			if (RawUrl != Url) {
				sb.Append($"\tResult: {Url}\n");
			}
			if (RawUrl != null) {
				sb.Append($"\tRaw: {RawUrl}\n");
			}

			if (Caption != null) {
				sb.Append($"\tCaption: {Caption}\n");
			}

			if (Similarity.HasValue) {
				sb.Append($"\tSimilarity: {Similarity / 100:P}\n");
			}

			if (Width.HasValue && Height.HasValue) {
				sb.Append($"\tResolution: {Width}x{Height}\n");
			}

			foreach (string s in ExtendedInfo) {
				sb.Append($"\t{s}\n");
			}

			// if (ExtendedResults.Count > 0) {
			// 	sb.AppendFormat("\tExtended results: {0}\n", ExtendedResults.Count);
			// }

			return sb.ToString();
		}

		private IList<FullSearchResult> FromExtendedResult(IReadOnlyList<ISearchResult> results)
		{
			var rg = new FullSearchResult[results.Count];

			for (int i = 0; i < rg.Length; i++) {
				var    result = results[i];
				string name   = $"Extended result #{i}";

				var sr = new FullSearchResult(Color, name, result.Url, result.Similarity)
				{
					Width   = result.Width,
					Height  = result.Height,
					Caption = result.Caption
				};

				rg[i] = sr;
			}


			return rg;
		}
	}
}