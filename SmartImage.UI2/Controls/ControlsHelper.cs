using System;
using System.Linq;
using Avalonia.Controls;
using Kantan.Utilities;

namespace SmartImage.UI2.Controls;

public static class ControlsHelper
{

	public static void SyncFlagsSelection<T>(this ListBox lb, T value) where T : struct, Enum
	{
		if (lb.ItemsSource is not { } items) {
			return;
		}

		lb.SelectedItems?.Clear();

		foreach (T t in items.OfType<T>().Where(t => value.HasFlag(t))) {
			lb.SelectedItems?.Add(t);
		}
	}

	/// <summary>
	/// Applies a <see cref="ListBox.SelectionChanged"/> event to a <see cref="Flags"/> enum value.
	/// </summary>
	public static T ApplyFlagsSelectionChanged<T>(this ListBox lb, SelectionChangedEventArgs e, T orig)
		where T : struct, Enum
	{
		var added    = e.AddedItems.OfType<T>().Aggregate(default(T), EnumHelper.Or);
		var removed  = e.RemovedItems.OfType<T>().Aggregate(default(T), EnumHelper.Or);
		var selected = lb.SelectedItems?.OfType<T>().Aggregate(default(T), EnumHelper.Or) ?? default;


		orig = selected.And(orig);
		orig = orig.And(removed.Not());
		orig = orig.Or(added);

		var setFlags = orig.GetSetFlags();
		
		foreach (var flag in setFlags) {
			lb.SelectedItems?.Add(flag);
		}

		return orig;
	}

}