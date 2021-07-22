using JetBrains.Annotations;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using Kantan.Model;
using Kantan.Utilities;
using Novus.Utilities;
using SmartImage.Lib.Engines.Model;

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
		Extraneous,

		Cooldown
	}


	/// <summary>
	/// Describes a search result
	/// </summary>
	public class SearchResult : IViewable
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
		/// Indicates whether this result is detailed.
		/// <para></para>
		/// If filtering is enabled (i.e., <see cref="SearchConfig.Filtering"/> is <c>true</c>), this determines whether the
		/// result is filtered.
		/// </summary>
		public bool IsNonPrimitive => (Status != ResultStatus.Extraneous && PrimaryResult.Url != null);

		/// <summary>
		/// The time spent processing this result.
		/// </summary>
		public TimeSpan? ProcessingTime { get; set; }

		/// <summary>
		/// The time spent retrieving raw data from the engine (<see cref="Engine"/>), if applicable.
		/// </summary>
		public TimeSpan? RetrievalTime { get; set; }

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


		public void Consolidate()
		{
			PrimaryResult = ReflectionHelper.Consolidate(PrimaryResult, OtherResults);
		}


		public override string ToString() => Strings.ViewString(this);


		public Dictionary<string, object> View
		{
			get
			{
				var map = new Dictionary<string, object>();

				if (PrimaryResult.Url != null) {
					map.Add(nameof(PrimaryResult), PrimaryResult);
				}

				map.Add("Raw", RawUri);

				if (OtherResults.Count != 0) {
					map.Add("Other image results", OtherResults.Count);
				}

				if (ErrorMessage != null) {
					map.Add("Error", ErrorMessage);
				}

				if (!IsSuccessful) {
					map.Add("Status", Status);
				}

				//

				var time  = new StringBuilder();
				var total = TimeSpan.Zero;

				if (RetrievalTime.HasValue) {
					var poll = RetrievalTime.Value.TotalSeconds;
					total += RetrievalTime.Value;
					time.Append($"({poll:F3} retrieval)").Append(' ');
				}

				if (ProcessingTime.HasValue) {
					var process = ProcessingTime.Value.TotalSeconds;
					total += ProcessingTime.Value;
					time.Append($"({process:F3} processing)").Append(' ');
				}

				time.Append($"({total.TotalSeconds:F3} total)");

				map.Add("Time", time);

				return map;
			}
		}
	}
}