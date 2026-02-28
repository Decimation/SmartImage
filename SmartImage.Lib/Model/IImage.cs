// Author: Deci | Project: SmartImage.Lib | Name: IImage.cs
// Date: 2026/01/09 @ 17:01:13

using SixLabors.ImageSharp.Formats;
using SmartImage.Lib.Engines.Results;

// ReSharper disable UnusedMemberInSuper.Global

namespace SmartImage.Lib.Model;

public interface IImage : ISize, ISimilarity, IHashable
{

	IImageFormat ImageFormat => Image?.Metadata.DecodedImageFormat;

	[MNNW(true, nameof(ImageFormat), nameof(Image))]
	bool HasImageFormat => ImageFormat != null;

	[MN]
	ISImage Image { get; }

	[MNNW(true, nameof(Image), nameof(ImageFormat))]
	bool HasImage => Image != null;

}