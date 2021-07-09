using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using SimpleCore.Model;
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

		public bool Notification { get; set; } = true;


		public bool NotificationImage { get; set; } = false;

		public override string ToString()
		{
			var sb = new ExtendedStringBuilder();
			sb.Append("Search engines", SearchEngines);
			sb.Append("Priority engines", PriorityEngines);
			sb.Append("Filtering", Filtering);
			sb.Append("Notification", Notification);
			sb.Append("Notification image", NotificationImage);

			return sb.ToString();
		}
		
	}
}