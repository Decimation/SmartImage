using JetBrains.Annotations;
using SimpleCore.Utilities;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;

#pragma warning disable IDE0066

namespace SmartImage.Lib.Searching
{
	public enum ResultStatus
	{
		/// <summary>
		/// Succeeded in parsing/retrieving result
		/// </summary>
		Success,


		/// <summary>
		/// No results found
		/// </summary>
		NoResults,

		/// <summary>
		/// Server unavailable
		/// </summary>
		Unavailable,

		/// <summary>
		/// Failed to parse/retrieve results
		/// </summary>
		Failure,

		/// <summary>
		/// Result is extraneous
		/// </summary>
		Extraneous
	}


	/// <summary>
	/// Describes a search result
	/// </summary>
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

		/// <summary>
		/// Undifferentiated URI
		/// </summary>
		public Uri RawUri { get; set; }


		/// <summary>
		/// The <see cref="BaseSearchEngine"/> that returned this result
		/// </summary>
		public BaseSearchEngine Engine { get; init; }

		/// <summary>
		/// Result status
		/// </summary>
		public ResultStatus Status { get; set; }

		/// <summary>
		/// Error message; if applicable
		/// </summary>
		[CanBeNull]
		public string ErrorMessage { get; set; }

		/// <summary>
		/// Indicates whether this result is non-primitive
		/// </summary>
		/// <remarks>Primitive results are non-detailed results (results with little information)</remarks>
		public bool IsNonPrimitive => Status != ResultStatus.Extraneous && (PrimaryResult.Url != null);


		public bool IsSuccessful
		{
			get
			{
				switch (Status) {
					case ResultStatus.Failure:
					case ResultStatus.Unavailable:
						return false;

					case ResultStatus.Success:
					case ResultStatus.NoResults:
					case ResultStatus.Extraneous:
						return true;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		public SearchResult(BaseSearchEngine engine)
		{
			Engine = engine;

			PrimaryResult = new ImageResult();
			OtherResults  = new List<ImageResult>();
		}


		public string ToString(bool name)
		{
			var sb = new ExtendedStringBuilder();

			if (name) {
				sb.AppendLine($"[{Engine.Name}] :: ({Status}; {(!IsNonPrimitive ? RANK_P : RANK_S)})");

			}
			else {
				sb.Append("\n");

			}

			if (PrimaryResult.Url != null) {
				//var    resStr    = sb.IndentFields(PrimaryResult.ToString());

				var    resStr    = PrimaryResult.ToString(true);
				string separator = Strings.Indentation + Strings.Separator;

				sb.Append($"{resStr}\n{separator}\n");
			}

			//========================================================================//

			var sb2 = new ExtendedStringBuilder();
			sb2.Append("Raw", RawUri);
			sb2.Append("Other image results", OtherResults, $"{OtherResults.Count}");
			sb2.Append("Error", ErrorMessage);

			return sb.Append(sb.IndentFields(sb2.ToString())).ToString();
		}

		public override string ToString() => ToString(true);

		internal const string RANK_P = "P";

		internal const string RANK_S = "S";
	}
}