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
		/// Filters any non-primitive results.
		/// Filtered results are determined by <see cref="SearchResult.IsNonPrimitive"/>.
		/// </summary>
		public bool Filtering { get; set; } = true;
		

		public override string ToString()
		{
			var sb = new ExtendedStringBuilder();
			sb.Append(nameof(SearchEngines), SearchEngines);
			sb.Append(nameof(PriorityEngines), PriorityEngines);
			sb.Append(nameof(Filtering), Filtering);

			return sb.ToString();
		}
	}
}