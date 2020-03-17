using System;
using JetBrains.Annotations;
using SmartImage.Utilities;

namespace SmartImage.Model
{
	public sealed class SearchResult
	{
		public string Url { get; }
		
		public string Name { get; }

		public float? Similarity { get; internal set; }

		public bool Success => Url != null;

		public SearchResult(string url, string name)
		{
			Url  = url;
			Name = name;
		}

		[CanBeNull]
		public string[] ExtendedInfo { get; internal set; }


		public override string ToString()
		{
			// redundant
			var cleanUrl = Success ? Url : null;

			if (Similarity.HasValue) {
				return string.Format("{0}: [{1}] {2}", Name, Similarity, cleanUrl);
			}
			
			return string.Format("{0}: {1}", Name, cleanUrl);
		}
	}
}