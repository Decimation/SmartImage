// Author: Deci | Project: SmartImage.Lib | Name: ISize.cs
// Date: 2026/02/21 @ 18:02:14

namespace SmartImage.Lib.Model;

public interface ISize
{

	/// <summary>
	///     Image width
	/// </summary>
	int? Width { get; set; }

	/// <summary>
	///     Image height
	/// </summary>
	int? Height { get; set; }

	[MNNW(true, nameof(Width), nameof(Height))]
	bool HasDimensions => Width.HasValue && Height.HasValue;

}