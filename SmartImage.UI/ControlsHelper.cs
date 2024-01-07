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
using Kantan.Utilities;
using Novus.FileTypes;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Model;

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

		if (originalBitmap.UriSource != null) {
			bitmapImage.UriSource = originalBitmap.UriSource;
		}

		else if (originalBitmap.StreamSource != null) {
			bitmapImage.StreamSource = originalBitmap.StreamSource;
		}

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
			if (src.HasFlag(t)) {
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
		// var rg = lb.ItemsSource.OfType<SearchEngineOptions>().ToArray();

		var ai = e.AddedItems.OfType<SearchEngineOptions>()
			.Aggregate(default(SearchEngineOptions), Func);

		var ri = e.RemovedItems.OfType<SearchEngineOptions>()
			.Aggregate(default(SearchEngineOptions), Func);

		var si = lb.SelectedItems.OfType<SearchEngineOptions>().ToArray();

		var siv = si.Aggregate(default(SearchEngineOptions), Func);

		orig &= siv;
		orig &= (~ri);
		orig |= ai;

		return orig;

		static SearchEngineOptions Func(SearchEngineOptions n, SearchEngineOptions l)
			=> n | l;
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

	public static string FormatDescription(string name, UniSource uni, int? w, int? h)
	{
		string bytes = FormatSize(uni);

		string i;

		i = FormatDimensions(w, h);

		return $"{name} ⇉ [{uni.FileType}] [{bytes}] • {i}";
	}

	public static string FormatSize(UniSource uni)
	{
		string bytes;

		if (!uni.Stream.CanRead) {
			bytes = "???";
		}
		else bytes = FormatHelper.FormatBytes(uni.Stream.Length);

		return bytes;
	}

	public static (bool ctrl, bool alt, bool shift) GetModifiers()
	{
		var ctrl  = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
		var alt   = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);
		var shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
		return (ctrl, alt, shift);
	}

	public static string FormatDimensions(int? w, int? h)
	{
		if (w.HasValue && h.HasValue)
		{
			return $"{w}\u00d7{h}";
		}

		return String.Empty;
	}

	internal const string STR_NA = "-";

}