#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using SimpleCore.CommandLine;
using SmartImage.Utilities;

#pragma warning disable HAA0502, HAA0302, HAA0505, HAA0601, HAA0301, HAA0501

namespace SmartImage.Searching.Model
{
	/// <summary>
	///     Contains search result and information
	/// </summary>
	public sealed class SearchResult : NConsoleOption, ISearchResult
	{
		public SearchResult(ISearchEngine engine, string? url, float? similarity = null)
			: this(engine.Color, engine.Name, url, similarity) { }

		public SearchResult(Color color, string name, string? url, float? similarity = null)
		{
			Url = url;
			Name = name;
			Color = color;

			Similarity = similarity;
			ExtendedInfo = new List<string>();
			ExtendedResults = new List<ISearchResult>();
		}

		public override Color Color { get; set; }

		public override string? Data => ToString();

		/// <summary>
		/// Result name
		/// </summary>
		public override string Name { get; set; }


		/// <summary>
		/// Raw search url
		/// </summary>
		public string? RawUrl { get; set; }

		public bool Success => Url != null;

		/// <summary>
		/// Extended information about the image, results, and other related metadata
		/// </summary>
		public List<string> ExtendedInfo { get; }

		/// <summary>
		///     Direct source matches and other extended results
		/// </summary>
		/// <remarks>This list is used if there are multiple results</remarks>
		public List<ISearchResult> ExtendedResults { get; }

		/// <summary>
		/// Opens result in browser
		/// </summary>
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

		public override Func<object?>? AltFunction { get; set; }

		public override Func<object?>? CtrlFunction { get; set; }

		public string? Url { get; set; }

		public float? Similarity { get; set; }

		public int? Width { get; set; }

		public int? Height { get; set; }

		
		public string? Caption { get; set; }

		private IEnumerable<SearchResult> FromExtendedResult(IReadOnlyList<ISearchResult> results)
		{
			var rg = new SearchResult[results.Count];

			for (int i = 0; i < rg.Length; i++) {
				var result = results[i];
				var name = String.Format("Extended result #{0}", i);

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

			ExtendedResults.AddRange(bestImages);

			AltFunction = () =>
			{
				var rg = FromExtendedResult(bestImages);


				NConsole.IO.HandleOptions(rg);

				return null;

			};
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			char success = Success ? NConsole.RAD_SIGN : NConsole.MUL_SIGN;
			string altStr = ExtendedResults.Count > 0 ? NConsole.IO.ALT_DENOTE : string.Empty;

			sb.AppendFormat("{0} {1}\n", success, altStr);


			if (Success && RawUrl != Url)
			{
				sb.AppendFormat("\tResult: {0}\n", Url);
			}
			else if (RawUrl != null)
			{
				sb.AppendFormat("\tRaw: {0}\n", RawUrl);
			}

			if (Caption != null)
			{
				sb.AppendFormat("\tCaption: {0}\n", Caption);
			}

			if (Similarity.HasValue)
			{
				sb.AppendFormat("\tSimilarity: {0:P}\n", Similarity / 100);
			}

			if (Width.HasValue && Height.HasValue)
			{
				sb.AppendFormat("\tResolution: {0}x{1}\n", Width, Height);
			}

			foreach (string s in ExtendedInfo)
			{
				sb.AppendFormat("\t{0}\n", s);
			}

			if (ExtendedResults.Count > 0)
			{
				sb.AppendFormat("\tExtended results: {0}\n", ExtendedResults.Count);
			}

			return sb.ToString();
		}
	}
}