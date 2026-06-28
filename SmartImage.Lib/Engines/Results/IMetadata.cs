// Author: Deci | Project: SmartImage.Lib | Name: IMetadata.cs
// Date: 2026/03/07 @ 01:03:45

using SmartImage.Lib.Model;

namespace SmartImage.Lib.Engines.Results;

public interface IMetadata : IDimensions
{

	/// <summary>
	/// Title/caption of this result
	/// </summary>
	[MN]
	string Title { get; }

	/// <summary>
	/// Media source of this result (e.g., anime, movie, game, etc.)
	/// </summary>
	[MN]
	string Source { get; }

	/// <summary>
	/// Artist or author
	/// </summary>
	[MN]
	string Artist { get; }

	/// <summary>
	///     Image description
	/// </summary>
	[MN]
	string Description { get; }

	/// <summary>
	/// Character(s) depicted in the image
	/// </summary>
	[MN]
	string Character { get; }

	/// <summary>
	/// Site which returned this result
	/// </summary>
	[MN]
	string Site { get; }

	/// <summary>
	/// Timestamp of the image.
	/// </summary>
	DateTime? Time { get; }

}