// Read S SmartImage.UI IImageProvider.cs
// 2023-09-13 @ 5:27 PM

using System;
using System.Windows.Media.Imaging;

namespace SmartImage.UI.Model;

[Flags]
public enum ImageSourceProperties
{

	None        = 0,
	CanDownload = 1 << 0,

}

public static class Ext
{

	public static bool HasFlagFast(this ImageSourceProperties r, ImageSourceProperties x)
	{
		return (r & x) != 0;
	}

}

public interface IGuiImageSource
{

	public Lazy<BitmapImage?> Image { get; set; }

	ImageSourceProperties Properties { get; set; }

	bool HasImage { get; }

	bool CanLoadImage { get; }

	bool IsThumbnail { get; }

	int? Width { get; }

	int? Height { get; }

	bool HasPropert(ImageSourceProperties p)
	{
		return Ext.HasFlagFast(Properties, p);
	}

	BitmapImage? LoadImage();

}