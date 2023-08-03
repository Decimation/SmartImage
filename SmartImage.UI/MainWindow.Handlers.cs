// Read S SmartImage.UI MainWindow.Handlers.cs
// 2023-07-23 @ 11:50 AM

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Flurl;
using Kantan.Net.Utilities;
using Novus.OS;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Impl.Upload;
using Color = System.Drawing.Color;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartImage.UI;

public partial class MainWindow
{
	#region

	#region

	private void Tb_Input_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (Interlocked.CompareExchange(ref _status, S_OK, S_NO) == S_NO) {
			return;
		}

		var txt = InputText;
		var ok  = SearchQuery.IsValidSourceType(txt);

		if (ok /*&& !IsInputReady()*/) {
			Application.Current.Dispatcher.InvokeAsync(() => SetQueryAsync(txt));
		}

		Btn_Run.IsEnabled = ok;
		e.Handled         = true;
	}

	private void Tb_Input_TextInput(object sender, TextCompositionEventArgs e) { }

	private void Tb_Input_DragOver(object sender, DragEventArgs e)
	{
		e.Handled = true;
	}

	private void Tb_Input_PreviewDragOver(object sender, DragEventArgs e)
	{
		e.Handled = true;
	}

	private void Tb_Input_Drop(object sender, DragEventArgs e)
	{
		var files1 = e.GetFilesFromDrop();

		EnqueueAsync(files1);
		var f1 = files1.FirstOrDefault();

		if (!string.IsNullOrWhiteSpace(f1)) {
			InputText = f1;

		}

		e.Handled = true;

	}

	private void Tb_Info_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		var s = Query.Uni.Value.ToString();

		if (Query.Uni.IsFile) {
			FileSystem.ExploreFile(s);
		}
		else if (Query.Uni.IsUri) {
			HttpUtilities.TryOpenUrl(s);

		}
	}

	private void Tb_Upload_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		HttpUtilities.TryOpenUrl(Query.Upload);
	}

	#endregion

	#region

	private void Lv_Queue_Drop(object sender, DragEventArgs e)
	{
		var files = e.GetFilesFromDrop();

		EnqueueAsync(files);
		e.Handled = true;
	}

	private void Lv_Queue_DragOver(object sender, DragEventArgs e)
	{
		e.Handled = true;
	}

	private void Lv_Queue_PreviewDragOver(object sender, DragEventArgs e)
	{
		e.Handled = true;
	}

	private void Lv_Queue_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.OriginalSource != sender) {
			return;
		}

		if (e.AddedItems.Count > 0) {

			Restart();
			var i = e.AddedItems[0] as string;
			InputText = i;
			// Next(i);

		}
	}

	private void Lv_Queue_KeyDown(object sender, KeyEventArgs e) { }

	#endregion

	private void Btn_Run_Click(object sender, RoutedEventArgs e)
	{
		// await SetQueryAsync(InputText);
		Btn_Run.IsEnabled = false;
		// Clear(true);

		Application.Current.Dispatcher.InvokeAsync(RunAsync);
	}

	private async void Btn_Clear_Click(object sender, RoutedEventArgs e)
	{
		// var ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
		ClearResults(true);
	}

	private void Btn_Restart_Click(object sender, RoutedEventArgs e)
	{
		var ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
		Restart(ctrl);
		Queue.Clear();
	}

	private void Btn_Restart_MouseEnter(object sender, MouseEventArgs e)
	{
		e.Handled = true;
	}

	private void Btn_Restart_MouseLeave(object sender, MouseEventArgs e)
	{
		e.Handled = true;
	}

	private void Btn_Next_Click(object sender, RoutedEventArgs e)
	{
		Next();
	}

	private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
	{
		Cancel();
		ReloadToken();
	}

	private void Btn_Run_Loaded(object sender, RoutedEventArgs e)
	{
		// Btn_Run.IsEnabled = false;
	}

	private void Btn_Reset_Click(object sender, RoutedEventArgs e)
	{
		Restart(true);
		ClearQueryControls();
	}

	#region

	private void Lv_Results_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		if (Lv_Results.SelectedItem is ResultItem si) {
			si.Open();
		}
	}

	private void Lv_Results_MouseRightButtonDown(object sender, MouseButtonEventArgs e) { }

	private void Lv_Results_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.AddedItems.Count > 0) {
			if (e.AddedItems[0] is ResultItem ri) {
				ChangeInfo2(ri);
			}

		}

		e.Handled = true;
	}

	private void Lv_Results_KeyDown(object sender, KeyEventArgs e)
	{

		var ctrl  = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
		var alt   = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);
		var shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

		var key = e.Key;

		switch (key) {
			case Key.D when ctrl:
				Application.Current.Dispatcher.InvokeAsync(
					() => DownloadResultAsync(((UniResultItem) Lv_Results.SelectedItem)));

				break;
			case Key.S when ctrl:

				Application.Current.Dispatcher.InvokeAsync(
					() => ScanResultAsync(((ResultItem) Lv_Results.SelectedItem)));

				break;
		}
	}

	#endregion

	#region

	private void Lb_Engines_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		Lb_Engines.HandleEnumOption(e, (ai, ri) =>
		{
			Config.SearchEngines |= (ai);
			Config.SearchEngines &= ~ri;
		});
	}

	private void Lb_Engines2_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		Lb_Engines2.HandleEnumOption(e, (ai, ri) =>
		{
			Config.PriorityEngines |= (ai);
			Config.PriorityEngines &= ~ri;
		});
	}

	private void Cb_Clipboard_Checked(object sender, RoutedEventArgs e)
	{
		// Config.Clipboard = !Config.Clipboard;
	}

	private void Cb_AutoSearch_Checked(object sender, RoutedEventArgs e)
	{
		// Config.AutoSearch = !Config.AutoSearch;
	}

	private void Cb_OpenRaw_Checked(object sender, RoutedEventArgs e)
	{
		// Config.OpenRaw = !Config.OpenRaw;
	}

	private void Cb_ContextMenu_Checked(object sender, RoutedEventArgs e)
	{
		if (!e.IsLoaded()) {
			return;
		}

		AppUtil.HandleContextMenu(!AppUtil.IsContextMenuAdded);

	}

	private void Rb_UploadEngine_Catbox_Checked(object sender, RoutedEventArgs e)
	{
		if (!e.IsLoaded() || e.OriginalSource != sender) {
			return;
		}

		BaseUploadEngine.Default = CatboxEngine.Instance;
	}

	private void Rb_UploadEngine_Litterbox_Checked(object sender, RoutedEventArgs e)
	{
		if (!e.IsLoaded() || e.OriginalSource != sender) {
			return;
		}

		BaseUploadEngine.Default = CatboxEngine.Instance;

	}

	#endregion

	#region

	private void Wnd_Main_Loaded(object sender, RoutedEventArgs e)
	{
		if (UseClipboard) {
			m_cbDispatch.Start();

		}

	}

	private void Wnd_Main_Unloaded(object sender, RoutedEventArgs e) { }

	private void Wnd_Main_Closed(object sender, EventArgs e)        { }
	private void Wnd_Main_Closing(object sender, CancelEventArgs e) { }

	#endregion

	#endregion
	
	private void OpenItem_Click(object sender, RoutedEventArgs e)
	{
		var ri = ((ResultItem) Lv_Results.SelectedItem);
		ri.Open();
	}

	private void DownloadItem_Click(object sender, RoutedEventArgs e)
	{
		if (Lv_Results.SelectedItem is UniResultItem uri) {
			Application.Current.Dispatcher.InvokeAsync(
				() => DownloadResultAsync(uri));

		}
		e.Handled = true;
	}
	private void ScanItem_Click(object sender, RoutedEventArgs e)
	{
		Application.Current.Dispatcher.InvokeAsync(() => ScanResultAsync(((ResultItem) Lv_Results.SelectedItem)));
		e.Handled = true;
	}
}