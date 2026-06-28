// Author: Deci | Project: SmartImage.Lib | Name: IImage.cs
// Date: 2026/01/09 @ 17:01:13

using SixLabors.ImageSharp.Formats;
using SmartImage.Lib.Model;


// ReSharper disable UnusedMemberInSuper.Global

namespace SmartImage.Lib.Images;

public interface IImage : IDimensions, ISimilarity, IHashable
{
	[MN]
	IImageFormat ImageFormat => Image?.Metadata.DecodedImageFormat;

	[MNNW(true, nameof(ImageFormat), nameof(Image))]
	bool HasImageFormat => ImageFormat != null;

	[MN]
	ISImage Image { get; }

	[MNNW(true, nameof(Image), nameof(ImageFormat))]
	bool HasImage => Image != null;

}