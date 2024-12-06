// Read S SmartImage.UI IImageProvider.cs
// 2023-09-13 @ 5:27 PM

using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SmartImage.UI.Model;

public interface IBitmapImageSource
{

	public BitmapSource? Image { get; internal set; }

	bool HasImage { get; }

	bool CanLoadImage { get; }

	bool IsThumbnail { get; }

	int? Width { get; }

	int? Height { get; }

	bool LoadImage();

}