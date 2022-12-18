using System.Diagnostics;
using Kantan.Console;
using Kantan.Net.Utilities;
using SmartImage.Lib.Engines;
using Terminal.Gui;

// ReSharper disable InconsistentNaming

namespace SmartImage.Shell;

internal static partial class UI
{
	internal static bool QueueProgress(CancellationTokenSource cts, ProgressBar pbr, Action<object>? f = null)
	{
		return ThreadPool.QueueUserWorkItem((state) =>
		{
			while (state is CancellationToken { IsCancellationRequested: false }) {
				pbr.Pulse();
				f?.Invoke(state);
				// Thread.Sleep(TimeSpan.FromMilliseconds(100));
			}

		}, cts.Token);
	}

	internal static Button CreateLinkButton(Dialog d, string text, string? url = null, Action? urlAction = null)
	{
		var b = new Button()
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

	internal static void SetLabelStatus(Label lbl, bool? b)
	{

		switch (b) {
			case null:
				lbl.Text        = UI.NA;
				lbl.ColorScheme = UI.Cs_NA;
				break;
			case true:
				lbl.Text        = UI.OK;
				lbl.ColorScheme = UI.Cs_Ok;
				break;
			case false:
				lbl.Text        = UI.Err;
				lbl.ColorScheme = UI.Cs_Err;
				break;
		}
	}

	internal static void OnEngineSelected(ListViewItemEventArgs args, ref SearchEngineOptions e, ListView lv)
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

		lv.FromEnum(e);
		
		lv.SetNeedsDisplay();
		Debug.WriteLine($"{val} {args.Item} -> {e} {isMarked}", nameof(OnEngineSelected));
	}

}