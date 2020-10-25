#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using SimpleCore.CommandLine;
using SimpleCore.Net;
using SimpleCore.Utilities;
using SimpleCore.Win32;
using SmartImage.Utilities;

#pragma warning disable HAA0502, HAA0302, HAA0505, HAA0601, HAA0301, HAA0501, HAA0101

namespace SmartImage.Searching.Model
{
	/// <summary>
	///     Contains search result and information
	/// </summary>
	public sealed class SearchResult : NConsoleOption, ISearchResult
	{
		


		public const char ATTR_SUCCESS = NConsole.CHECK_MARK;

		public const char ATTR_EXTENDED_RESULTS = NConsole.ARROW_UP_DOWN;

		public const char ATTR_DOWNLOAD = NConsole.ARROW_DOWN;

		public override Color Color { get; set; }

		public override string Data => ToString();

		/// <summary>
		///     Result name
		/// </summary>
		public override string Name { get; set; }


		/// <summary>
		///     Raw, undifferentiated search url
		/// </summary>
		public string? RawUrl { get; set; }

		

		/// <summary>
		///     Extended information about the image, results, and other related metadata
		/// </summary>
		public List<string> ExtendedInfo { get; }

		/// <summary>
		///     Direct source matches and other extended results
		/// </summary>
		/// <remarks>This list is used if there are multiple results</remarks>
		public List<SearchResult> ExtendedResults { get; }

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

		/// <summary>
		/// Displays <see cref="ExtendedResults"/> if any
		/// </summary>
		public override NConsoleFunction? AltFunction { get; set; }

		/// <summary>
		/// Downloads image, if possible, and opens it in Explorer highlighted
		/// </summary>
		public override NConsoleFunction? CtrlFunction
		{
			get
			{
				return () =>
				{
					if (!IsImage) {
						bool ok = NConsole.IO.ReadConfirm(
							$"Link may not be an image [{MimeType ?? "?"}]. Download anyway?");

						if (!ok) {
							return null;
						}
					}

					string? path = Network.DownloadUrl(Url);

					NConsole.WriteSuccess("Downloaded to {0}", path);

					// Open folder with downloaded file selected
					FileOperations.ExploreFile(path);


					NConsole.IO.WaitForSecond();

					return null;
				};
			}
		}


		public bool IsProcessed { get; set; }

		public bool IsImage { get; set; }

		public string? MimeType { get; set; }

		public SearchResult(ISearchEngine engine, string url, float? similarity = null)
			: this(engine.Color, engine.Name, url, similarity) { }

		public SearchResult(Color color, string name, string url, float? similarity = null)
		{
			Url = url;
			Name = name;
			Color = color;

			Similarity = similarity;
			ExtendedInfo = new List<string>();
			ExtendedResults = new List<SearchResult>();
		}


		public string Url { get; set; }

		public float? Similarity { get; set; }

		public int? Width { get; set; }

		public int? Height { get; set; }


		public string? Caption { get; set; }

		private IList<SearchResult> FromExtendedResult(IReadOnlyList<ISearchResult> results)
		{
			var rg = new SearchResult[results.Count];

			for (int i = 0; i < rg.Length; i++) {
				var result = results[i];
				string name = String.Format("Extended result #{0}", i);

				var sr = new SearchResult(Color, name, result.Url, result.Similarity)
				{
					Width = result.Width,
					Height = result.Height,
					Caption = result.Caption
				};

				rg[i] = sr;
			}


			return rg;
		}

		public void AddExtendedResults(ISearchResult[] bestImages)
		{
			// todo?

			var rg = FromExtendedResult(bestImages);

			//ExtendedResults.AddRange(bestImages);

			ExtendedResults.AddRange(rg);

			foreach (var result in rg) {
				SearchClient.RunProcessingTask(result);
			}

			AltFunction = () =>
			{
				//var rg = FromExtendedResult(bestImages);

				NConsole.IO.HandleOptions(ExtendedResults);

				return null;

			};
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			string attrSuccess = ATTR_SUCCESS.ToString();

			string attrExtendedResults = ExtendedResults.Count > 0 ? ATTR_EXTENDED_RESULTS.ToString() : String.Empty;

			string attrDownload;

			if (!IsProcessed) {
				attrDownload = "-";
			}
			else {
				attrDownload = IsImage ? ATTR_DOWNLOAD.ToString() : NConsole.BALLOT_X + ATTR_DOWNLOAD.ToString();
			}


			sb.AppendFormat("{0} {1} {2}\n", attrSuccess, attrExtendedResults, attrDownload);


			if (RawUrl != Url) {
				sb.AppendFormat("\tResult: {0}\n", Url);
			}
			else if (RawUrl != null) {
				sb.AppendFormat("\tRaw: {0}\n", RawUrl);
			}

			if (Caption != null) {
				sb.AppendFormat("\tCaption: {0}\n", Caption);
			}

			if (Similarity.HasValue) {
				sb.AppendFormat("\tSimilarity: {0:P}\n", Similarity / 100);
			}

			if (Width.HasValue && Height.HasValue) {
				sb.AppendFormat("\tResolution: {0}x{1}\n", Width, Height);
			}

			foreach (string s in ExtendedInfo) {
				sb.AppendFormat("\t{0}\n", s);
			}

			if (ExtendedResults.Count > 0) {
				sb.AppendFormat("\tExtended results: {0}\n", ExtendedResults.Count);
			}

			return sb.ToString();
		}
	}
}