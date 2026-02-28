// Author: Deci | Project: SmartImage.Lib | Name: IResultMetadata.cs
// Date: 2026/02/21 @ 18:02:51

using SmartImage.Lib.Model;

namespace SmartImage.Lib.Engines.Results;

public interface IResultMetadata : ISize
{

	/// <summary>
	///     Title/caption of this result
	/// </summary>
	string Title { get; }

	/// <summary>
	///     Media source of this result (e.g., anime, movie, game, etc.)
	/// </summary>
	string Source { get; }

	/// <summary>
	///     Artist or author
	/// </summary>
	string Artist { get; }

	/// <summary>
	///     Image description
	/// </summary>
	string Description { get; }

	/// <summary>
	///     Character(s) depicted in the image
	/// </summary>
	string Character { get; }

	/// <summary>
	///     Site which returned this result
	/// </summary>
	string Site { get; }

	/// <summary>
	///     Timestamp of the image.
	/// </summary>
	DateTime? Time { get; }

	
}