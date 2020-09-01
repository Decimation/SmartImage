#region

using System;
using System.Collections.Generic;
using System.Text;
using SimpleCore.Utilities;
using SmartImage.Model;

#endregion

namespace SmartImage.Searching
{
	public sealed class SearchResult : ConsoleOption
	{
		public SearchResult(string url, string name, float? similarity = null)
		{
			Url = url;
			Name = name;
			Similarity = similarity;
			ExtendedInfo = new List<string>();
		}

		public string Url { get; }

		public override string? ExtendedName
		{
			get
			{
				return Format();
			}
		}

		public override string Name { get; }


		public float? Similarity { get; internal set; }

		public bool Success => Url != null;

		public List<string> ExtendedInfo { get; }

		public override Func<object> Function
		{
			get
			{
				return () =>
				{
					WebAgent.OpenUrl(Url);
					return null;
				};
			}
		}


		public override string ToString()
		{
			return String.Format("{0}: {1}", Name, Url);
		}

		public string Format()
		{
			var sb = new StringBuilder();
			sb.AppendFormat("{0}\n",Success ? CliOutput.RAD_SIGN : CliOutput.MUL_SIGN);

			if (Success) {
				
				sb.AppendFormat("\tResult url: {0}\n", Url);
			}

			if (Similarity.HasValue) {
				sb.AppendFormat("\tSimilarity: {0:P}\n", Similarity);
			}

			foreach (string s in ExtendedInfo) {
				sb.AppendFormat("\t{0}\n", s);
			}

			return sb.ToString();
		}
	}
}