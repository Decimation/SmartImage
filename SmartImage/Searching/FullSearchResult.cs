#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;
using Novus.Win32;
using Novus.Win32.FileSystem;
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
		public const char ATTR_DOWNLOAD = Formatting.ARROW_DOWN;

		public const char ATTR_EXTENDED_RESULTS = Formatting.ARROW_UP_DOWN;

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

					string? path = Network.DownloadUrl(Url);

					NConsole.WriteSuccess("Downloaded to {0}", path);

					// Open folder with downloaded file selected
					Files.ExploreFile(path);


					NConsoleIO.WaitForSecond();

					return null;
				};
			}
		}

		// public bool? IsImage
		// {
		// 	get;
		// 	internal set;
		// }

		


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
				NConsoleIO.HandleOptions(ExtendedResults);

				return null;
			};
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			string attrSuccess = ATTR_SUCCESS.ToString();

			string attrExtendedResults = ExtendedResults.Count > 0 ? ATTR_EXTENDED_RESULTS.ToString() : String.Empty;


			// string attrDownload = IsImage.HasValue
			// 	? (IsImage.Value ? ATTR_DOWNLOAD.ToString() : Formatting.BALLOT_X.ToString())
			// 	: Formatting.SUN.ToString();

			string attrDownload = ATTR_DOWNLOAD.ToString();

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