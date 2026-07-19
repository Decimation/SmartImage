// Author: Deci | Project: SmartImage.Rdx | Name: ItemMetadataProperties.cs
// Date: 2026/06/27 @ 21:06:20

#nullable disable
using SmartImage;
using SmartImage.Lib.Model;

namespace SmartImage.Lib.Engines.Results.Enums;

[Flags]
public enum ItemMetadataProperties
{

	None = 0,

	Name = 1 << 0,

	/// <summary>
	/// <see cref="IUrl.Url"/>
	/// </summary>
	Url = 1 << 1,

	/// <summary>
	/// <see cref="ISimilarity.Similarity"/>
	/// </summary>
	Similarity = 1 << 2,

	/// <summary>
	/// <see cref="IResultMetadata.Title"/>
	/// </summary>
	Title = 1 << 3,

	/// <summary>
	/// <see cref="IResultMetadata.Source"/>
	/// </summary>
	Source = 1 << 4,

	/// <summary>
	/// <see cref="IResultMetadata.Artist"/>
	/// </summary>
	Artist = 1 << 5,

	/// <summary>
	/// <see cref="IResultMetadata.Description"/>
	/// </summary>
	Description = 1 << 6,

	/// <summary>
	/// <see cref="IResultMetadata.Character"/>
	/// </summary>
	Character = 1 << 7,

	/// <summary>
	/// <see cref="IResultMetadata.Site"/>
	/// </summary>
	Site = 1 << 8,

	/// <summary>
	/// <see cref="IResultMetadata.Time"/>
	/// </summary>
	Time = 1 << 9,

	/// <summary>
	/// <see cref="IHashable.Hash"/>
	/// </summary>
	Hash = 1 << 10,

	/// <summary>
	/// <see cref="IDimensions.Width"/>, <see cref="IDimensions.Height"/>
	/// </summary>
	Dimensions = 1 << 11,

}