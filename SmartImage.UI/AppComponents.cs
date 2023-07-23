// Read S SmartImage.UI AppControls.cs
// 2023-07-23 @ 4:16 PM

using System.Windows.Media.Imaging;

namespace SmartImage.UI;

public static class AppComponents
{
	private const int WIDTH  = 20;
	private const int HEIGHT = 20;

	public static readonly BitmapImage accept = new BitmapImage(ControlsHelper.GetComponentUri("accept.png"))
	{
		CacheOption = BitmapCacheOption.OnLoad,

	}.ResizeBitmap(WIDTH, HEIGHT);

	public static readonly BitmapImage exclamation = new BitmapImage(ControlsHelper.GetComponentUri("exclamation.png"))
	{
		CacheOption = BitmapCacheOption.OnLoad,

	}.ResizeBitmap(WIDTH, HEIGHT);

	public static readonly BitmapImage help = new BitmapImage(ControlsHelper.GetComponentUri("help.png"))
	{
		CacheOption = BitmapCacheOption.OnLoad,

	}.ResizeBitmap(WIDTH, HEIGHT);
}