// Read S SmartImage.UI AppControls.cs
// 2023-07-23 @ 4:16 PM

using System.Windows.Media.Imaging;

namespace SmartImage.UI;

public static class AppControls
{
	private const int WIDTH  = 20;
	private const int HEIGHT = 20;

	public static readonly BitmapImage accept = new BitmapImage(FormsHelper.GetAsset("accept.png"))
	{
		CacheOption = BitmapCacheOption.OnLoad,

	}.ResizeBitmap(WIDTH, HEIGHT);

	public static readonly BitmapImage exclamation = new BitmapImage(FormsHelper.GetAsset("exclamation.png"))
	{
		CacheOption = BitmapCacheOption.OnLoad,

	}.ResizeBitmap(WIDTH, HEIGHT);

	public static readonly BitmapImage help = new BitmapImage(FormsHelper.GetAsset("help.png"))
	{
		CacheOption = BitmapCacheOption.OnLoad,

	}.ResizeBitmap(WIDTH, HEIGHT);
}