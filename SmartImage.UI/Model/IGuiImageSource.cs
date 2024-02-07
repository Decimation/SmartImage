// Read S SmartImage.UI IImageProvider.cs
// 2023-09-13 @ 5:27 PM

using System.Windows.Media.Imaging;
using Flurl;
using Kantan.Monad;
using SmartImage.Lib.Model;

namespace SmartImage.UI.Model;

public interface IGuiImageSource : IBaseImageSource
{

	public BitmapImage? Image { get; set; }

}

