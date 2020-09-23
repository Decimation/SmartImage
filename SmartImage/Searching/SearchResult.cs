#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using SimpleCore.Utilities;
using SmartImage.Utilities;

namespace SmartImage.Searching
{
	public sealed class SearchResult : ConsoleOption
	{
		public SearchResult(ISearchEngine engine, string url, float? similarity = null) : this(engine.Color,
			engine.Name, url, similarity) { }

		public SearchResult(ConsoleColor color, string name, string url, float? similarity = null)
		{
			Url = url;
			Name = name;
			Color = color;

			Similarity = similarity;
			ExtendedInfo = new List<string>();
			FilteredMatchResults = new List<string>();

		}

		public override ConsoleColor Color { get; }

		public string Url { get; }

		public override string? ExtendedName => Format();

		public override string Name { get; }


		public float? Similarity { get; internal set; }

		public bool Success => Url != null;

		public List<string> ExtendedInfo { get; }

		/// <summary>
		/// Direct source matches
		/// </summary>
		public List<string> FilteredMatchResults { get; }

		public override Func<object> Function
		{
			get
			{
				return () =>
				{
					NetworkUtilities.OpenUrl(Url);
					return null;
				};
			}
		}

		public override Func<object>? AltFunction { get; internal set; }


		public override string ToString()
		{
			return String.Format("{0}: {1}", Name, Url);
		}

		internal const string ALT_DENOTE = "[Alt]";

		private string Format()
		{
			var sb = new StringBuilder();

			char success = Success ? CliOutput.RAD_SIGN : CliOutput.MUL_SIGN;
			string hasAlt = AltFunction != null ? ALT_DENOTE : string.Empty;

			sb.AppendFormat("{0} {1}\n", success, hasAlt);

			if (Success) {

				sb.AppendFormat("\tResult url: {0}\n", Url);
			}

			if (Similarity.HasValue) {
				sb.AppendFormat("\tSimilarity: {0:P}\n", Similarity);
			}

			foreach (string s in ExtendedInfo) {
				sb.AppendFormat("\t{0}\n", s);
			}

			for (int i = 0; i < FilteredMatchResults.Count; i++) {
				string extraResult = FilteredMatchResults[i];
				sb.AppendFormat("\tMatch result #{0}: {1}\n",i, extraResult);
			}

			return sb.ToString();
		}
	}
}