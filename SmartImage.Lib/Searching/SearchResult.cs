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
		Success,
		NoResults,
		Unavailable,
		Failure,
		Extraneous
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

		/// <summary>
		/// Undifferentiated URI
		/// </summary>
		public Uri RawUri { get; set; }

		public BaseSearchEngine Engine { get; init; }

		public ResultStatus Status { get; set; }

		[CanBeNull]
		public string ErrorMessage { get; set; }

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
			var sb = new ExtendedStringBuilder() { };

			if (name) {
				sb.AppendLine($"[{Engine.Name}] :: ({Status}; {(!IsNonPrimitive ? RANK_P : RANK_S)})");

			}
			else {
				sb.Append("\n");

			}

			if (PrimaryResult.Url != null) {
				//var    resStr    = sb.IndentFields(PrimaryResult.ToString());

				var    resStr    = PrimaryResult.ToString(true);
				string separator = Strings.Indent + Strings.Separator;

				sb.Append($"{resStr}\n{separator}\n");
			}

			//========================================================================//

			var sb2 = new ExtendedStringBuilder() { };
			sb2.Append("Raw", RawUri);
			sb2.Append("Other image results", OtherResults, $"{OtherResults.Count}");
			sb2.Append("Error", ErrorMessage);

			return sb.Append(sb.IndentFields(sb2.ToString())).ToString();
		}

		public override string ToString()
		{
			return ToString(true);
		}

		internal const string RANK_P = "P";

		internal const string RANK_S = "S";
	}
}