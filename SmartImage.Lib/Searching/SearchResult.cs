using System.Collections.Generic;
using SmartImage.Lib.Engines;

namespace SmartImage.Lib.Searching
{
	public class SearchResult
	{
		public ImageResult PrimaryResult { get; set; }

		public List<ImageResult> OtherResults { get; init; }

		public string RawUrl { get; init; }

		public SearchEngine Engine { get; init; }

		public SearchResult(SearchEngine engine)
		{
			Engine = engine;

			PrimaryResult = new ImageResult();
			OtherResults  = new List<ImageResult>();
		}

		public override string ToString()
		{
			return $"{Engine.Name}: {PrimaryResult} ({OtherResults.Count})";
		}
	}
}