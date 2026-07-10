// Author: Deci | Project: SmartImage.Rdx | Name: ItemMetadataProperties.cs
// Date: 2026/06/27 @ 21:06:20

#nullable disable
using SmartImage.Lib.Model;

namespace SmartImage.Lib.Engines.Results;

[Flags]
public enum ItemMetadataProperties
{

	None = 0,

	Name = 1 << 0,

	/// <summary>
	/// <see cref="IUrl"/>
	/// </summary>
	Url = 1 << 1,

	/// <summary>
	/// <see cref="ISimilarity"/>
	/// </summary>
	Similarity = 1 << 2,

	/// <summary>
	/// <see cref="IResultMetadata"/>
	/// </summary>
	Title = 1 << 3,

	/// <summary>
	/// <see cref="IResultMetadata"/>
	/// </summary>
	Source = 1 << 4,

	/// <summary>
	/// <see cref="IResultMetadata"/>
	/// </summary>
	Artist = 1 << 5,

	/// <summary>
	/// <see cref="IResultMetadata"/>
	/// </summary>
	Description = 1 << 6,

	/// <summary>
	/// <see cref="IResultMetadata"/>
	/// </summary>
	Character = 1 << 7,

	/// <summary>
	/// <see cref="IResultMetadata"/>
	/// </summary>
	Site = 1 << 8,

	/// <summary>
	/// <see cref="IResultMetadata"/>
	/// </summary>
	Time = 1 << 9,

	/// <summary>
	/// <see cref="IHashable"/>
	/// </summary>
	Hash = 1 << 10,

	/// <summary>
	/// <see cref="IDimensions"/>
	/// </summary>
	Dimensions = 1 << 11,

}