using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using SimpleCore.Utilities;
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

		public BaseSearchEngine Engine { get; init; }

		public ResultStatus Status { get; set; }

		[CanBeNull]
		public string ErrorMessage { get; set; }


		public SearchResult(BaseSearchEngine engine)
		{
			Engine = engine;

			PrimaryResult = new ImageResult();
			OtherResults  = new List<ImageResult>();

		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.AppendLine($"[{Engine.Name}] ({Status})".AddColor(Color.Aquamarine));

			if (PrimaryResult.Url != null) {
				sb.Append($"{PrimaryResult}\n");
			}

			if (RawUri != null) {
				sb.AppendFormat($"Raw: {RawUri.ToString().Truncate()}\n");

			}

			if (OtherResults.Any()) {
				sb.AppendFormat($"Other: {OtherResults.Count}\n");

			}

			if (ErrorMessage != null) {
				sb.Append($"Error: {ErrorMessage}\n");
			}

			return sb.ToString();
		}
	}
}