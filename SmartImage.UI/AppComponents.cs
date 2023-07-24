// Read S SmartImage.UI AppControls.cs
// 2023-07-23 @ 4:16 PM

using System;
using System.Windows;
using System.Windows.Media.Imaging;
// ReSharper disable InconsistentNaming

namespace SmartImage.UI;

public static class AppComponents
{
	private const int WIDTH  = 20;
	private const int HEIGHT = 20;

	static AppComponents() { }

	public static BitmapImage Load(string name, int w = WIDTH, int h = HEIGHT)
	{
		return new BitmapImage(GetComponentUri(name))
		{
			CacheOption = BitmapCacheOption.OnLoad,

		}.ResizeBitmap(w, h);
	}

	public static Uri GetComponentUri(string n, string resources = "Resources")
	{
		return new Uri($"pack://application:,,,/{AppUtil.Assembly.GetName().Name};component/{resources}/{n}");
	}

	#region 

	public static readonly BitmapImage accept = Load("accept.png");

	public static readonly BitmapImage exclamation = Load("exclamation.png");

	public static readonly BitmapImage help = Load("help.png");

	public static readonly BitmapImage information = Load("information.png");

	public static readonly BitmapImage picture = Load("picture.png");

	public static readonly BitmapImage picture_save = Load("picture_save.png");

	#endregion
}