// Author: Deci | Project: SmartImage.Lib | Name: IDimensions.cs
// Date: 2026/02/21 @ 18:02:14

namespace SmartImage.Lib.Model;

public interface IDimensions
{

	/// <summary>
	///     Image width
	/// </summary>
	int? Width { get; set; }

	/// <summary>
	///     Image height
	/// </summary>
	int? Height { get; set; }

	[MNNW(true, nameof(Width.Value), nameof(Height.Value))]
	bool HasDimensions => Width is not null && Height is not null;

}