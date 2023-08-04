using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SmartImage.Lib.Engines;

namespace SmartImage.UI;

public static class ControlsHelper
{
	public static bool IsDoubleClick(this MouseButtonEventArgs e)
	{
		return e.ClickCount == 2;
	}

	public static BitmapImage ResizeBitmap(this BitmapImage originalBitmap, int newWidth, int newHeight)
	{
		// Calculate the scale factors for width and height
		double scaleX = (double) newWidth / originalBitmap.PixelWidth;
		double scaleY = (double) newHeight / originalBitmap.PixelHeight;

		// Create a new Transform to apply the scale factors
		Transform transform = new ScaleTransform(scaleX, scaleY);

		// Create a new TransformedBitmap with the original BitmapImage and the scale Transform
		var resizedBitmap = new TransformedBitmap(originalBitmap, transform);

		// Create a new BitmapImage and set it as the source of the resized image
		var bitmapImage = new BitmapImage();
		bitmapImage.BeginInit();
		bitmapImage.UriSource         = originalBitmap.UriSource;
		bitmapImage.DecodePixelWidth  = newWidth;
		bitmapImage.DecodePixelHeight = newHeight;
		bitmapImage.CacheOption       = BitmapCacheOption.OnLoad;
		bitmapImage.EndInit();

		return bitmapImage;
	}

	public static bool IsLoaded(this RoutedEventArgs e)
	{
		var b = e is { Source: FrameworkElement { IsLoaded: true } fx };

		return b;
	}

	public static void HandleEnum<T>(this ListBox lb, T src) where T : struct, Enum
	{
		foreach (T t in lb.ItemsSource.OfType<T>()) {
			if (src.HasFlag((T) t)) {
				lb.SelectedItems.Add(t);
			}
			else {
				lb.SelectedItems.Remove(t);
			}
		}
	}

	/*static T parse<T>(IList x) where T : struct, Enum
	{
		return x.OfType<T>().Aggregate(default(T), (n, l) => (T) (object) (Convert.ToInt32(n) | Convert.ToInt32(l)));

	}*/

	public static SearchEngineOptions HandleEnum(this ListBox lb, SelectionChangedEventArgs e,
	                                             SearchEngineOptions orig)
	{
		var rg = lb.ItemsSource.OfType<SearchEngineOptions>().ToArray();

		var ai = e.AddedItems.OfType<SearchEngineOptions>()
			.Aggregate(default(SearchEngineOptions), (n, l) => n | l);

		var ri = e.RemovedItems.OfType<SearchEngineOptions>()
			.Aggregate(default(SearchEngineOptions), (n, l) => n | l);

		var si = lb.SelectedItems.OfType<SearchEngineOptions>().ToArray();

		var siv = si.Aggregate(default(SearchEngineOptions), (n, l) => n | l);

		orig &= siv;
		orig &= (~ri);
		orig |= ai;

		return orig;

	}

	public static string[] GetFilesFromDrop(this DragEventArgs e)
	{
		if (e.Data.GetDataPresent(DataFormats.FileDrop)) {

			if (e.Data.GetData(DataFormats.FileDrop, true) is string[] files
			    && files.Any()) {

				return files;

			}
		}

		return Array.Empty<string>();
	}
}