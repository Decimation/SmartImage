// Read S SmartImage UI.cs
// 2023-02-14 @ 12:12 AM

#region

using System.Collections;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Kantan.Console;
using Kantan.Net.Utilities;
using SmartImage.Lib.Engines;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;
using Color=Terminal.Gui.Color;
#endregion

// ReSharper disable InconsistentNaming

namespace SmartImage.Mode.Shell.Assets;

internal static partial class UI
{
	internal static bool QueueProgress(CancellationTokenSource cts, ProgressBar pbr, Action<object>? f = null)
	{
		return ThreadPool.QueueUserWorkItem(state =>
		{
			while (state is CancellationToken { IsCancellationRequested: false }) {
				pbr.Pulse();
				f?.Invoke(state);
				Task.Delay(TimeSpan.FromSeconds(0.5));
				// Thread.Sleep(TimeSpan.FromMilliseconds(100));
			}

		}, cts.Token);
	}

	internal static Button CreateLinkButton(this Dialog d, string text, string? url = null, Action? urlAction = null)
	{
		var b = new Button
		{
			Text     = text,
			AutoSize = true,
		};

		urlAction ??= () => HttpUtilities.TryOpenUrl(url);

		b.Clicked += () =>
		{
			urlAction();
			d.SetNeedsDisplay();
		};

		return b;
	}

	internal static void SetLabelStatus(this Label lbl, bool? b)
	{

		switch (b) {
			case null:
				lbl.Text        = NA;
				lbl.ColorScheme = Cs_NA;
				break;
			case true:
				lbl.Text        = OK;
				lbl.ColorScheme = Cs_Ok;
				break;
			case false:
				lbl.Text        = Err;
				lbl.ColorScheme = Cs_Err;
				break;
		}
	}

	public static void FromEnum<TEnum>(this ListView lv, TEnum e) where TEnum : struct, Enum
	{
		var list = lv.Source.ToList<TEnum>().ToArray();

		for (var i = 0; i < list.Length; i++) {
			// var flag = Enum.Parse<TEnum>(list[i].ToString());
			// var mark = e.HasFlag(flag);
			TEnum e2   = list[i];
			var   mark = e.HasFlag(e2);

			if (e2.Equals(default(TEnum))) {
				// Debug.WriteLine($"Skipping {default(TEnum)}");
				continue;
			}

			// Debug.WriteLine($"{e}, {e2} -> {mark}");
			lv.Source.SetMark(i, mark);
		}
	}

	public static void ClearBy<T>(this ListView lv, Predicate<T> p)
	{
		var cc = lv.Source.ToList<T>().ToArray();

		for (int i = 0; i < lv.Source.Length; i++) {
			lv.Source.SetMark(i, p(cc[i]));
		}
	}

	public static TEnum GetEnum<TEnum>(this IListDataSource lv, TEnum t = default) where TEnum : struct, Enum
	{
		var m = lv.GetItems<TEnum>();

		TEnum t2 = t;

		Debug.Assert(Unsafe.SizeOf<TEnum>() == Unsafe.SizeOf<int>());

		unsafe {
			var ptr = (int*) Unsafe.AsPointer(ref t2);

			foreach (var t1 in m) {
				TEnum tv  = t1.Value;
				var   val = (int*) Unsafe.AsPointer(ref tv);

				if (t1.IsMarked) {
					*ptr |= *val;

				}
				else {
					*ptr &= ~*val;
				}
			}

		}

		return t2;
	}

	internal static void OnEngineSelected(ListView lv, ListViewItemEventArgs lvie, ref SearchEngineOptions e)
	{
		var l = lv.Source.ToList<SearchEngineOptions>().ToArray();

		for (int i = 0; i < l.Length; i++) {
			if (lv.Source.IsMarked(i)) {
				switch (l[i]) {
					case SearchEngineOptions.None:
						e = SearchEngineOptions.None;
						goto ret;
					case SearchEngineOptions.All:
						e = SearchEngineOptions.All;
						goto ret;
				}

				e |= l[i];
			}
		}

		ret:
		var v = ((SearchEngineOptions) lvie.Value);

		if (lv.Source.IsMarked(lvie.Item)) {
			e |= v;
		}
		else {
			e &= ~v;
		}

		lv.FromEnum(e);
	}

	/*internal static void OnEngineSelected(ListViewItemEventArgs args, ref SearchEngineOptions e, ListView lv)
	{
		var val = (SearchEngineOptions) args.Value;

		var isMarked = lv.Source.IsMarked(args.Item);

		bool b = val == SearchEngineOptions.None;

		if (isMarked) {
			if (b) {
				e = val;

				for (int i = 1; i < lv.Source.Length; i++) {
					lv.Source.SetMark(i, false);
				}
			}
			else {
				e |= val;
			}
		}
		else {
			e &= ~val;
		}

		if (!b) {
			lv.Source.SetMark(0, false);
		}

		lv.FromEnum2(e);

		lv.SetNeedsDisplay();
		Debug.WriteLine($"{val} {args.Item} -> {e} {isMarked}", nameof(OnEngineSelected));
	}*/

	public static ColorScheme NormalizeHot(this ColorScheme cs)
	{
		cs.HotFocus  = cs.Focus;
		cs.HotNormal = cs.Normal;
		return cs;
	}

	private const int BRIGHT_DELTA = 0b1000;

	public static Color ToBrightVariant(this Color c)
	{
		// +8
		return (Color) ((int) c + BRIGHT_DELTA);
	}

	public static Color ToDarkVariant(this Color c)
	{
		// +8
		return (Color) ((int) c - BRIGHT_DELTA);
	}
}