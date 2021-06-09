using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using SimpleCore.Utilities;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib
{
	/// <summary>
	/// Contains configuration for <see cref="SearchClient"/>
	/// </summary>
	/// <remarks>Search config is only applicable when used in <see cref="SearchClient"/></remarks>
	public sealed class SearchConfig
	{
		/// <summary>
		/// Search query
		/// </summary>
		public ImageQuery Query { get; set; }

		/// <summary>
		/// Search engines to use
		/// </summary>
		public SearchEngineOptions SearchEngines { get; set; }

		/// <summary>
		/// Priority engines
		/// </summary>
		public SearchEngineOptions PriorityEngines { get; set; }

		/// <summary>
		/// Filters any non-primitive results; <see cref="SearchResult.IsNonPrimitive"/>
		/// </summary>
		public bool Filter { get; set; } = true;

		/// <summary>
		/// Scan for direct image links; <see cref="ImageResult.FindDirectImages"/>
		/// </summary>
		public bool DirectUri { get; set; } = false;


		public override string ToString()
		{
			var sb = new ExtendedStringBuilder();
			sb.AppendLine("Config");
			sb.Append(nameof(SearchEngines), SearchEngines);
			sb.Append(nameof(PriorityEngines), PriorityEngines);
			sb.Append(nameof(Filter), Filter);
			sb.Append(nameof(DirectUri), DirectUri);

			return sb.ToString();
		}
	}
}