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
using RestSharp;
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
	public class SearchResult : IOutline
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
					case ResultStatus.Cooldown:
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

		/// <summary>
		/// The time taken to retrieve results from the engine
		/// </summary>
		public TimeSpan? RetrievalTime { get; set; }

		public void Consolidate()
		{
			PrimaryResult = ReflectionHelper.Consolidate(PrimaryResult, OtherResults);
		}


		public override string ToString() => Strings.OutlineString(this);
		
		public Dictionary<string, object> Outline
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

				if (RetrievalTime.HasValue) {
					map.Add("Time", $"(retrieval: {RetrievalTime.Value.TotalSeconds:F3})");
				}

				return map;
			}
		}
	}
}