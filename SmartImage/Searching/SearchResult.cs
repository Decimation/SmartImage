#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleCore.Utilities;
using SimpleCore.Win32.Cli;
using SmartImage.Searching.Model;
using SmartImage.Shell;
using SmartImage.Utilities;

namespace SmartImage.Searching
{
	/// <summary>
	/// Contains search result and information
	/// </summary>
	public sealed class SearchResult : ConsoleOption, IExtendedSearchResult
	{
		public SearchResult(ISearchEngine engine, string url, float? similarity = null)
			: this(engine.Color, engine.Name, url, similarity) { }

		public SearchResult(ConsoleColor color, string name, string url, float? similarity = null)
		{
			Url = url;
			Name = name;
			Color = color;

			Similarity = similarity;
			ExtendedInfo = new List<string>();
			ExtendedResults = new List<IExtendedSearchResult>();
		}

		public override ConsoleColor Color { get; internal set; }


		/// <summary>
		/// Best match
		/// </summary>
		public string Url { get; }

		// todo: create a specific url field with the original url

		public override string? Data => Format();

		public override string Name { get; internal set; }

		/// <summary>
		/// Image similarity
		/// </summary>
		public float? Similarity { get; set; }

		public int? Width { get; set; }
		public int? Height { get; set; }
		public string? Caption { get; set; }

		public bool Success => Url != null;

		public List<string> ExtendedInfo { get; }

		/// <summary>
		/// Direct source matches, other extended results
		/// </summary>
		public List<IExtendedSearchResult> ExtendedResults { get; }

		public override Func<object?> Function
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

		public override Func<object?>? AltFunction { get; internal set; }


		private SearchResult[] FromExtendedResult(IReadOnlyList<IExtendedSearchResult> results)
		{

			var rg = new SearchResult[results.Count];

			for (int i = 0; i < rg.Length; i++) {
				var result = results[i];

				var sr = new SearchResult(Color, "Extended result", result.Url, result.Similarity)
				{
					Width = result.Width,
					Height = result.Height,
					Caption = result.Caption
				};
				rg[i] = sr;

			}


			return rg;
		}

		public void AddExtendedInfo(IExtendedSearchResult[] bestImages)
		{
			// todo?

			//

			ExtendedResults.AddRange(bestImages);


			AltFunction = () =>
			{
				var rg = FromExtendedResult(bestImages);


				ConsoleIO.HandleConsoleOptions(rg);

				return null;

			};
		}

		public override string ToString()
		{
			return String.Format("{0}: {1}", Name, Url);
		}


		private string Format()
		{
			var sb = new StringBuilder();

			char success = Success ? CliOutput.RAD_SIGN : CliOutput.MUL_SIGN;
			string altStr = AltFunction != null ? ConsoleIO.ALT_DENOTE : string.Empty;

			sb.AppendFormat("{0} {1}\n", success, altStr);

			if (Success) {

				sb.AppendFormat("\tResult: {0}\n", Url);
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