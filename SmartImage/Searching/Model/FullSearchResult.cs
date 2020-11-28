#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Novus.Win32;
using SimpleCore.Console.CommandLine;
using SimpleCore.Net;
using SimpleCore.Utilities;


#pragma warning disable HAA0601 // Value type to reference type conversion causing boxing allocation
#pragma warning disable HAA0602 // Delegate on struct instance caused a boxing allocation
#pragma warning disable HAA0603 // Delegate allocation from a method group
#pragma warning disable HAA0604 // Delegate allocation from a method group

#pragma warning disable HAA0501 // Explicit new array type allocation
#pragma warning disable HAA0502 // Explicit new reference type allocation
#pragma warning disable HAA0503 // Explicit new reference type allocation
#pragma warning disable HAA0504 // Implicit new array creation allocation
#pragma warning disable HAA0505 // Initializer reference type allocation
#pragma warning disable HAA0506 // Let clause induced allocation

#pragma warning disable HAA0301 // Closure Allocation Source
#pragma warning disable HAA0302 // Display class allocation to capture closure
#pragma warning disable HAA0303 // Lambda or anonymous method in a generic method allocates a delegate instance

#pragma warning disable HAA0401
#pragma warning disable HAA0101

namespace SmartImage.Searching.Model
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
					if (!IsImage) {
						bool ok = NConsoleIO.ReadConfirmation(
							$"Link may not be an image [{MimeType ?? "?"}]. Download anyway?");

						if (!ok) {
							return null;
						}
					}

					string? path = Network.DownloadUrl(Url);

					NConsole.WriteSuccess("Downloaded to {0}", path);

					// Open folder with downloaded file selected
					Files.ExploreFile(path);


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

		public bool IsImage { get; set; }

		public bool IsProcessed { get; set; }

		public string? MimeType { get; set; }

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

			foreach (var result in rg) {
				SearchClient.Client.RunProcessingTask(result);
			}

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

			string attrDownload;

			if (!IsProcessed) {
				attrDownload = Formatting.SUN.ToString();
			}
			else {
				attrDownload = IsImage ? ATTR_DOWNLOAD.ToString() : Formatting.BALLOT_X + ATTR_DOWNLOAD.ToString();
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

		private IList<FullSearchResult> FromExtendedResult(IReadOnlyList<ISearchResult> results)
		{
			var rg = new FullSearchResult[results.Count];

			for (int i = 0; i < rg.Length; i++) {
				var    result = results[i];
				string name   = String.Format("Extended result #{0}", i);

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