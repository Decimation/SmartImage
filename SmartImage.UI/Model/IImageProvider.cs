// Read S SmartImage.UI IImageProvider.cs
// 2023-09-13 @ 5:27 PM

using System.Windows.Media.Imaging;

namespace SmartImage.UI.Model;

public interface IImageProvider
{
	BitmapImage? Image        { get; set; }
	bool         HasImage     => Image != null;
	bool         CanLoadImage { get; }
	bool         LoadImage();
}