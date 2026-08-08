using System;
using System.Linq;
using Avalonia.Controls;
using Kantan.Utilities;

namespace SmartImage.UI2.Controls;

public static class ControlsHelper
{

	/// <summary>
	/// Synchronizes a multi-select <see cref="ListBox"/>'s selection with a <see cref="Flags"/> enum value.
	/// </summary>
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
		var added    = e.AddedItems.OfType<T>().Aggregate(default(T), Or);
		var removed  = e.RemovedItems.OfType<T>().Aggregate(default(T), Or);
		var selected = lb.SelectedItems?.OfType<T>().Aggregate(default(T), Or) ?? default;

		orig = And(selected, orig);
		orig = And(orig, Not(removed));
		orig = Or(orig, added);

		return orig;

		static T Or(T  a, T b) => (T) Enum.ToObject(typeof(T), Convert.ToInt64(a) | Convert.ToInt64(b));
		static T And(T a, T b) => (T) Enum.ToObject(typeof(T), Convert.ToInt64(a) & Convert.ToInt64(b));
		static T Not(T a) => (T) Enum.ToObject(typeof(T), ~Convert.ToInt64(a));
	}

}