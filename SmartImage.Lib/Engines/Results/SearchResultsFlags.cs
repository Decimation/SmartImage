// Author: Deci | Project: SmartImage.Lib | Name: SearchResultsFlags.cs
// Date: 2026/05/02 @ 08:05:20

namespace SmartImage.Lib.Engines.Results;

/// <summary>
/// Describes <see cref="SearchResult.Results"/>
/// </summary>
[Flags]
public enum SearchResultsFlags
{

	None = 0,

	/// <summary>
	/// Engine returned no results
	/// </summary>
	NoResults = 1 << 0,

	/// <summary>
	/// Result is extraneous
	/// </summary>
	Extraneous = 1 << 1,

}