// Author: Deci | Project: SmartImage.Lib | Name: SearchResponseStatus.cs
// Date: 2026/05/02 @ 08:05:22

namespace SmartImage.Lib.Engines.Results;

/// <summary>
/// Describes the response status from an engine
/// </summary>
public enum SearchResponseStatus
{

	/// <summary>
	/// N/A
	/// </summary>
	None = 0,

	/// <summary>
	/// Result obtained successfully
	/// </summary>
	Success,

	/// <summary>
	/// Engine is on cooldown due to too many requests
	/// </summary>
	Cooldown,

	/// <summary>
	/// Obtaining results failed due to an engine error
	/// </summary>
	Unknown,

	/// <summary>
	/// Query input invalid
	/// </summary>
	IllegalInput,

	/// <summary>
	/// Engine is unavailable
	/// </summary>
	Unavailable,
}