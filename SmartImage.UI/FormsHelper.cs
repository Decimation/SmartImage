using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Kantan.Utilities;
using SmartImage.Lib.Engines;

namespace SmartImage.UI;

public static class FormsHelper
{

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