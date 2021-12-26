using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Kantan.Model;
using Kantan.Text;
using Kantan.Utilities;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib;

/// <summary>
/// Contains configuration for <see cref="SearchClient"/>
/// </summary>
/// <remarks>Search config is only applicable when used in <see cref="SearchClient"/></remarks>
public sealed class SearchConfig : ConfigurationSection
{
	/// <summary>
	/// Search query
	/// </summary>
	public ImageQuery Query { get; set; }

	public SearchConfig() { }

	/// <summary>
	/// Search engines to use
	/// </summary>
	[ConfigurationProperty("engines", DefaultValue = SearchEngineOptions.All, IsRequired = true, IsKey = true)]
	public SearchEngineOptions SearchEngines { get; set; }

	/// <summary>
	/// Priority engines
	/// </summary>
	[ConfigurationProperty("priority-engines", DefaultValue = SearchEngineOptions.Auto, IsRequired = true,
	                       IsKey = true)]
	public SearchEngineOptions PriorityEngines { get; set; }

	/// <summary>
	/// Filters any non-primitive results.
	/// Filtered results are determined by <see cref="SearchResult.IsNonPrimitive"/>.
	/// </summary>
	[ConfigurationProperty("filtering", DefaultValue = true, IsRequired = true, IsKey = true)]
	public bool Filtering { get; set; } = true;

	/// <summary>
	/// <see cref="SearchClient.SearchCompleted"/>
	/// </summary>
	[ConfigurationProperty("notification", DefaultValue = true, IsRequired = true, IsKey = true)]
	public bool Notification { get; set; } = true;

	/// <summary>
	/// <see cref="SearchClient.SearchCompleted"/>
	/// </summary>

	[ConfigurationProperty("notification-image", DefaultValue = false, IsRequired = true, IsKey = true)]
	public bool NotificationImage { get; set; } = false;

	public bool OutputOnly { get; set; } = false;

	public override string ToString()
	{
		var sb = new StringBuilder();
		sb.Append("Search engines", SearchEngines);
		sb.Append("Priority engines", PriorityEngines);
		sb.Append("Filtering", Filtering);
		sb.Append("Notification", Notification);
		sb.Append("Notification image", NotificationImage);
		sb.Append("Output only", OutputOnly);


		return sb.ToString();
	}
}