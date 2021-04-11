using System;
using System.Collections.Generic;
using SmartImage.Lib.Engines;

namespace SmartImage.Lib.Searching
{
	public class SearchResult
	{
		public ImageResult PrimaryResult { get; set; }

		public List<ImageResult> OtherResults { get; set; }

		public Uri RawUri { get; set; }

		public SearchEngine Engine { get; init; }

		public TimeSpan Elapsed { get; set; }

		public SearchResult(SearchEngine engine)
		{
			Engine = engine;

			PrimaryResult = new ImageResult();
			OtherResults  = new List<ImageResult>();
		}

		public override string ToString()
		{
			return $"{Engine.Name}: {PrimaryResult} ({OtherResults.Count}) [{Elapsed.TotalSeconds:F3}]";
		}
	}
}