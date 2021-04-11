using System;
using System.Collections.Generic;
using System.Text;
using SmartImage.Lib.Engines;

namespace SmartImage.Lib.Searching
{
	public enum ResultStatus
	{
		Success,
		Failure
	}

	public class SearchResult
	{
		public ImageResult PrimaryResult { get; set; }

		public List<ImageResult> OtherResults { get; set; }

		public Uri RawUri { get; set; }

		public SearchEngine Engine { get; init; }

		public ResultStatus Status { get; set; }


		public SearchResult(SearchEngine engine)
		{
			Engine = engine;

			PrimaryResult = new ImageResult();
			OtherResults  = new List<ImageResult>();
			
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine($"[{Engine.Name}] ({Status})");
			sb.AppendFormat($"\t{PrimaryResult}\n");
			sb.AppendFormat($"\t{RawUri}\n");
			sb.AppendFormat($"\t{OtherResults.Count}\n");

			return sb.ToString();
		}
	}
}