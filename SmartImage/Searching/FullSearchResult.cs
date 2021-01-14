#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
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
		///     Displays <see cref="ExtendedResults" />, if any, in a new menu
		/// </summary>
		public override NConsoleFunction? AltFunction { get; set; }

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

					string? path = Network.DownloadUrl(Url);

					NConsole.WriteSuccess("Downloaded to {0}", path);

					// Open folder with downloaded file selected
					FileSystem.ExploreFile(path);

					NConsole.WaitForSecond();

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

		public string? Caption { get; set; }

		public int? Height { get; set; }

		public float? Similarity { get; set; }

		public string Url { get; set; }

		public int? Width { get; set; }
		

		public bool Filter { get; set; }
		
		

		public void AddExtendedResults(ISearchResult[] bestImages)
		{
			// todo?

			var rg = FromExtendedResult(bestImages);

			ExtendedResults.AddRange(rg);

			AltFunction = () =>
			{
				NConsole.ReadOptions(ExtendedResults);

				return null;
			};
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			string attrSuccess = Formatting.CHECK_MARK.ToString();


			string? ex = ExtendedResults.Count > 0
				? String.Format($"({ExtendedResults.Count})")
				: String.Empty;

			var fstr = Filter ? $"{Formatting.BALLOT_X}" : "";
			
			sb.Append($"{attrSuccess} {ex} {fstr}\n");


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
	}
}