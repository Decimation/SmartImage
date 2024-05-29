// Read S SmartImage.UI AppControls.cs
// 2023-07-23 @ 4:16 PM
global using R4 = SmartImage.Lib.Serialization;
global using R3 = SmartImage.Lib.Values;
global using R2 = SmartImage.UI.Resources;
global using R1 = SmartImage.Lib.Resources;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;
using JetBrains.Annotations;
using SmartImage.Lib.Utilities;

// ReSharper disable NullableWarningSuppressionIsUsed

// ReSharper disable InconsistentNaming

namespace SmartImage.UI.Controls;
#nullable enable
public static class AppComponents
{

	public const int WIDTH  = 20;
	public const int HEIGHT = 20;

	static AppComponents() { }

	public static BitmapImage LoadInline([CallerMemberName] string? name = null, int w = WIDTH, int h = HEIGHT,
	                                     string? ext = "png")
		=> Load(Path.ChangeExtension(name, ext)!, w, h);

	public static BitmapImage Load(string name, int w = WIDTH, int h = HEIGHT)
	{
		var bmp = new BitmapImage()
			{ };
		bmp.BeginInit();
		bmp.CacheOption = BitmapCacheOption.OnLoad;
		bmp.UriSource   = GetComponentUri(name);
		bmp.EndInit();
		bmp = bmp.ResizeBitmap(w, h);

		if (bmp.CanFreeze) {
			bmp.Freeze();
		}

		return bmp;
	}

	public static Uri GetComponentUri(string n, string resources = "Resources")
	{
		return new Uri($"pack://application:,,,/{Assembly.GetExecutingAssembly().GetName().Name};component/{resources}/{n}");
	}

	#region

	public static readonly BitmapImage accept = LoadInline();

	public static readonly BitmapImage exclamation = LoadInline();

	public static readonly BitmapImage help = LoadInline();

	public static readonly BitmapImage information = LoadInline();

	public static readonly BitmapImage picture  = LoadInline();
	public static readonly BitmapImage pictures = LoadInline();

	public static readonly BitmapImage picture_save  = LoadInline();
	public static readonly BitmapImage picture_error = LoadInline();
	public static readonly BitmapImage picture_empty = LoadInline();
	public static readonly BitmapImage picture_link  = LoadInline();

	public static readonly BitmapImage artwork = LoadInline();

	public static readonly BitmapImage image = LoadInline();

	public static readonly BitmapImage image_link = LoadInline();

	public static readonly BitmapImage link = LoadInline();

	public static readonly BitmapImage arrow_refresh = LoadInline();

	public static readonly BitmapImage clipboard_invoice = LoadInline();

	public static readonly BitmapImage asterisk_yellow = LoadInline();

	#endregion

	public static Bitmap BitmapImage2Bitmap(this BitmapImage bitmapImage)
	{
		// BitmapImage bitmapImage = new BitmapImage(new Uri("../Images/test.png", UriKind.Relative));

		using (MemoryStream outStream = new MemoryStream()) {
			BitmapEncoder enc = new BmpBitmapEncoder();
			enc.Frames.Add(BitmapFrame.Create(bitmapImage));
			enc.Save(outStream);
			Bitmap bitmap = new Bitmap(outStream);

			return new Bitmap(bitmap);
		}
	}

}