// Author: Deci | Project: SmartImage.Lib | Name: IImage.cs
// Date: 2025/08/20 @ 20:08:37

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;

namespace SmartImage.Lib.Images;

public interface IImageSource
{
	public ISImage Image { get; }

	[MNNW(true, nameof(Image))]
	public bool HasImage { get; }

	public IImageFormat ImageFormat { get; }

	[MNNW(true, nameof(ImageFormat))]
	public bool HasImageFormat { get; }

}