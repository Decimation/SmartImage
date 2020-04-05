using System;
using JetBrains.Annotations;
using SmartImage.Utilities;

namespace SmartImage.Model
{
	public sealed class SearchResult
	{
		public string Url { get; }
		
		public string Name { get; }
		
		public float? Similarity { get; }

		public bool Success => Url != null;

		public SearchResult(string url, string name, float? similarity = null)
		{
			Url  = url;
			Name = name;
			Similarity = similarity;
		}

		[CanBeNull]
		public string[] ExtendedInfo { get; internal set; }


		public override string ToString()
		{
			// redundant
			var cleanUrl = Success ? Url : null;

			return String.Format("{0}: {1}", Name, cleanUrl);
		}

		public static string Format(SearchResult result)
		{
			var str = result.ToString();

			int lim = Console.BufferWidth - (3 + 10);

			if (str.Length > lim) {
				str = str.Truncate(lim);
			}

			str += "...";

			return str;
		}
	}
}