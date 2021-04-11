using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartImage.Lib
{
	public class SearchResult
	{
		public ImageResult PrimaryResult { get; init; }

		public List<ImageResult> OtherResults { get; init; }

		public string RawUrl { get; init; }

		public SearchEngine Engine { get; init; }

		public SearchResult(SearchEngine engine)
		{
			Engine = engine;
		}

		public override string ToString()
		{
			return $"{Engine} -> {PrimaryResult} {RawUrl}";
		}
	}
}