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
		NoResults,
		Failure
	}

	public class SearchResult
	{
		/// <summary>
		/// Primary image result
		/// </summary>
		public ImageResult PrimaryResult { get; set; }

		/// <summary>
		/// Other image results
		/// </summary>
		public List<ImageResult> OtherResults { get; set; }


		public Uri RawUri { get; set; }

		public BaseSearchEngine Engine { get; init; }

		public ResultStatus Status { get; set; }

		[CanBeNull]
		public string ErrorMessage { get; set; }

		public bool IsPrimitive
		{
			get
			{
				//todo: WIP
				return PrimaryResult.Url == null;
			}
		}

		

		public SearchResult(BaseSearchEngine engine)
		{
			Engine = engine;

			PrimaryResult = new ImageResult();
			OtherResults  = new List<ImageResult>();

		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.AppendLine($"[{Engine.Name}] ({Status}; {(IsPrimitive ? "P" : "S")})");

			if (PrimaryResult.Url != null) {
				
				string s = new('-',20);

				sb.Append($"{PrimaryResult}\n{s}\n");
			}

			//========================================================================//

			if (RawUri != null) {
				sb.AppendFormat($"Raw: {RawUri.ToString().Truncate()}\n");

			}

			if (OtherResults.Any()) {
				sb.AppendFormat($"Other image results: {OtherResults.Count}\n");

			}

			if (ErrorMessage != null) {
				sb.Append($"Error: {ErrorMessage}\n");
			}

			return sb.ToString();
		}
	}
}