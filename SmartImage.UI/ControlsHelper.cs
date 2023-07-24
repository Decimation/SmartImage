using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Kantan.Utilities;
using SmartImage.Lib.Engines;

namespace SmartImage.UI;

public static class ControlsHelper
{
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

	public static string FormatBytes(long bytes)
	{
		string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

		if (bytes == 0) {
			return "0 " + suffixes[0];
		}

		const int newBase = 1000;

		int    magnitude    = (int) Math.Floor(Math.Log(bytes, newBase));
		double adjustedSize = bytes / Math.Pow(newBase, magnitude);

		return string.Format("{0:n2} {1}", adjustedSize, suffixes[magnitude]);
	}

	public static bool IsLoaded(this RoutedEventArgs e)
	{
		var b = e is { Source: FrameworkElement { IsLoaded: true } fx };

		return b;
	}

	public static void HandleEnumList<T>(this ListBox lb, T a, T b, bool add) where T : struct, Enum
	{

		if (a.Equals(b)) {
			var sf = EnumHelper.GetSetFlags(b);

			foreach (var seo in sf) {
				if (add) {
					lb.SelectedItems.Add(seo);
				}
				else {
					lb.SelectedItems.Remove(seo);
				}
			}
		}
	}

	public static void HandleEnumList<T>(this ListBox lb, List<T> rg, T src) where T : struct, Enum
	{
		foreach (T t in rg) {
			if (src.HasFlag((T) t)) {
				lb.SelectedItems.Add(t);
			}
		}
	}

	public static void HandleEnumOption(this ListBox lb, SelectionChangedEventArgs e,
	                                    Action<SearchEngineOptions, SearchEngineOptions> set)
	{
		var rg = lb.Items.OfType<SearchEngineOptions>().ToArray();

		var ai = e.AddedItems.OfType<SearchEngineOptions>()
			.Aggregate(default(SearchEngineOptions), (n, l) => n | l);

		var ri = e.RemovedItems.OfType<SearchEngineOptions>()
			.Aggregate(default(SearchEngineOptions), (n, l) => n | l);

		var si = lb.SelectedItems.OfType<SearchEngineOptions>().ToArray();

		lb.HandleEnumList(ai, SearchEngineOptions.All, true);

		if (ri.HasFlag(SearchEngineOptions.All)) {
			lb.UnselectAll();
		}

		lb.HandleEnumList(ai, SearchEngineOptions.Artwork, true);
		lb.HandleEnumList(ri, SearchEngineOptions.Artwork, false);

		set(ai, ri);

		Debug.WriteLine($"{ai} {si}");
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