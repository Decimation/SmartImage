// Author: Deci | Project: SmartImage.Lib | Name: SearchResponseFlags.cs
// Date: 2026/05/02 @ 08:05:22

namespace SmartImage.Lib.Engines.Results.Enums;

/// <summary>
/// Describes the response status from an engine
/// </summary>
[Flags]
public enum SearchResponseFlags
{

	/// <summary>
	/// N/A
	/// </summary>
	None = 0,

	/// <summary>
	/// Result obtained successfully
	/// </summary>
	Success = 1 << 0,

	/// <summary>
	/// Engine is on cooldown due to too many requests
	/// </summary>
	Cooldown = 1 << 1,

	/// <summary>
	/// Query input invalid
	/// </summary>
	IllegalInput = 1 << 2,

	/// <summary>
	/// Engine is unavailable
	/// </summary>
	Unavailable = 1 << 3,

	NoResults = 1 << 4,

	Unknown = 1 << 5,

	Error =  IllegalInput | Unavailable | Unknown
}