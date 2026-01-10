// Author: Deci | Project: SmartImage.Lib | Name: IImage.cs
// Date: 2026/01/09 @ 17:01:13

using SixLabors.ImageSharp.Formats;

namespace SmartImage.Lib.Model;

public interface IImage
{

	IImageFormat ImageFormat { get; }

	[MNNW(true, nameof(ImageFormat), nameof(Image))]
	bool HasImageFormat { get; }

	ISImage Image { get; }

	[MNNW(true, nameof(Image), nameof(ImageFormat))]
	bool HasImage { get; }

}