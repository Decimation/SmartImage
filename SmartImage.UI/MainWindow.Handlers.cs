// Read S SmartImage.UI MainWindow.Handlers.cs
// 2023-07-23 @ 11:50 AM

global using VBFS = Microsoft.VisualBasic.FileIO.FileSystem;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Flurl;
using Flurl.Http;
using Kantan.Net.Utilities;
using Kantan.Numeric;
using Microsoft.VisualBasic.FileIO;
using Novus.FileTypes;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Impl.Search;
using SmartImage.Lib.Engines.Impl.Search.Other;
using SmartImage.Lib.Engines.Impl.Upload;
using FileSystem = Novus.OS.FileSystem;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartImage.UI;

public partial class MainWindow
{
	#region

	#region

	private void Tb_Input_TextChanged(object sender, TextChangedEventArgs e)
	{
		// Debug.Assert(InputText == Queue[m_queuePos]);
		// Debug.Assert(Lv_Queue.SelectedValue.ToString() == InputText);
		// Debug.Assert(Lv_Queue.SelectedItem.ToString() == InputText);

		var nt = Tb_Input.Text;
		// var txt = InputText;
		var txt = nt;
		var ok  = SearchQuery.IsValidSourceType(txt);

		// QueueInsert(txt);

		CurrentQueueItem = txt;

		if (ok /*&& !IsInputReady()*/) {
			Application.Current.Dispatcher.InvokeAsync(UpdateQueryAsync);
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

		AddToQueueAsync(files1);
		var f1 = files1.FirstOrDefault();

		if (!string.IsNullOrWhiteSpace(f1)) {
			CurrentQueueItem = f1;

		}

		e.Handled = true;

	}

	private void Tb_Info_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		var s = Query.Uni.Value.ToString();

		if (string.IsNullOrWhiteSpace(s)) {
			return;
		}

		if (Query.Uni.IsFile) {
			// FileSystem.ExploreFile(s);

			FileSystem.Open(s);
		}
		else if (Query.Uni.IsUri) {
			FileSystem.Open(s);

		}
	}

	private void Tb_Upload_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		FileSystem.Open(Query.Upload);
	}

	#endregion

	#region

	private void Lv_Queue_Drop(object sender, DragEventArgs e)
	{
		var files = e.GetFilesFromDrop();

		AddToQueueAsync(files);
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

		// Debug.WriteLine($"{QueueSelectedIndex} {QueueSelectedItem}");
		// m_queuePos = Lv_Queue.SelectedIndex;

		if (e.AddedItems.Count > 0) {
			//todo
			Restart();

			if (e.AddedItems[0] is string i) {
				CurrentQueueItem = i;
				Debug.Assert(CurrentQueueItem.Equals(CurrentQueueItem));
			}

			// EnqueueAsync(new []{i});
			// Next(i);
			// Lv_Queue.SelectionChanged -= Lv_Queue_SelectionChanged;
			// Queue[m_queuePos]         =  i;
			// Lv_Queue.SelectionChanged += Lv_Queue_SelectionChanged;

		}

		e.Handled = true;
	}

	private void Lv_Queue_KeyDown(object sender, KeyEventArgs e) { }

	#endregion

	private void Btn_Run_Click(object sender, RoutedEventArgs e)
	{
		// await SetQueryAsync(InputText);
		Btn_Run.IsEnabled = false;
		// Clear(true);
		ClearResults(true);
		Application.Current.Dispatcher.InvokeAsync(RunAsync);
	}

	private void Btn_Clear_Click(object sender, RoutedEventArgs e)
	{
		// var ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
		ClearResults(true);
	}

	private void Btn_Reset_Click(object sender, RoutedEventArgs e)
	{
		Reset();
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

	private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
	{
		Cancel();
		ReloadToken();
	}

	private void Btn_Run_Loaded(object sender, RoutedEventArgs e)
	{
		// Btn_Run.IsEnabled = false;
	}

	private void Btn_Remove_Click(object sender, RoutedEventArgs e)
	{
		// var q   = MathHelper.Wrap(QueueSelectedIndex + 1, Queue.Count);

		var old = CurrentQueueItem;
		// TrySeekQueue(q);
		Queue.Remove(old);
		m_queries.TryRemove(old, out var sq);
		sq?.Dispose();
	}

	private void Btn_Delete_Click(object sender, RoutedEventArgs e)
	{
		Cancel();
		ClearResults();
		m_cbDispatch.Stop();
		var old = CurrentQueueItem;
		m_clipboard.Remove(old);
		CurrentQueueItem = String.Empty;
		m_queries.TryRemove(old, out var q);
		m_resultMap.TryRemove(Query, out var x);
		Query.Dispose();
		Queue.Remove(old);
		Img_Preview.Source = m_image = null;
		Query              = SearchQuery.Null;
		bool ok;

		try {
			VBFS.DeleteFile(old, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
			// FileSystem.SendFileToRecycleBin(old);
			ok = true;
		}
		catch (Exception exception) {
			Debug.WriteLine($"{exception}");
			ok = false;
		}

		m_cbDispatch.Start();
		Btn_Delete.IsEnabled = !ok;
		// FileSystem.SendFileToRecycleBin(InputText);
	}

	#region

	private void Lv_Results_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		CurrentResultItem.Open();
	}

	private void Lv_Results_MouseRightButtonDown(object sender, MouseButtonEventArgs e) { }

	private void Lv_Results_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.AddedItems.Count > 0) {

			var ai = e.AddedItems[0];

			switch (ai) {
				case UniResultItem uri:
					Img_Preview.Source = uri.Image;
					break;
				case ResultItem ri:

					Img_Preview.Source = m_image;

					ChangeInfo2(ri);
					break;

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
					() => DownloadResultAsync(((UniResultItem) CurrentResultItem)));

				break;
			case Key.S when ctrl:

				Application.Current.Dispatcher.InvokeAsync(() => ScanResultAsync(CurrentResultItem));
				break;
			case Key.Delete:
				if (CurrentResultItem == null) {
					return;
				}

				CurrentResultItem.Dispose();
				Results.Remove(CurrentResultItem);
				Img_Preview.Source = m_image;
				break;
			case Key.C when ctrl:
				Application.Current.Dispatcher.InvokeAsync(() =>
				{
					var text = CurrentResultItem.Url;
					m_clipboard.Add(text);
					Clipboard.SetText(text);
				});
				break;
			case Key.X when ctrl:
				Application.Current.Dispatcher.InvokeAsync(async () =>
				{
					var cri = CurrentResultItem;

					var p = Process.Start(new ProcessStartInfo("gallery-dl", $"-G {cri.Url}")
					{
						CreateNoWindow         = true,
						RedirectStandardOutput = true,
						RedirectStandardError  = true,
					});
					var s  = await p.StandardOutput.ReadToEndAsync();
					var s2 = s.Split(Environment.NewLine);
					var rg = new ConcurrentBag<UniSource>();
					await Parallel.ForEachAsync(s2, async (s1, token) =>
					{
						var uni = await UniSource.TryGetAsync(s1, ct: token);

						if (uni != null) {
							rg.Add(uni);
						}
					});
					cri.Result.Uni = rg.ToArray();

					for (int i = 0; i < s2.Length; i++) {
						var rii = new UniResultItem(cri, i)
						{
							StatusImage = AppComponents.picture,
							CanDownload = true,
							CanScan     = false,
							CanOpen     = true
						};
						Results.Insert(Results.IndexOf(cri) + 1 + i, rii);
					}
				});

				break;
		}

		e.Handled = true;
	}

	#endregion

	#region

	private void Lb_Engines_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		Lb_Engines.SelectionChanged -= Lb_Engines_SelectionChanged;

		var n = Lb_Engines.HandleEnum(e, Config.SearchEngines);

		Lb_Engines.HandleEnum(n);
		Config.SearchEngines = n;

		e.Handled                   =  true;
		Lb_Engines.SelectionChanged += Lb_Engines_SelectionChanged;
	}

	private void Lb_Engines2_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		Lb_Engines2.SelectionChanged -= Lb_Engines2_SelectionChanged;

		var n = Lb_Engines2.HandleEnum(e, Config.PriorityEngines);

		Lb_Engines2.HandleEnum(n);
		Config.PriorityEngines = n;

		e.Handled                    =  true;
		Lb_Engines2.SelectionChanged += Lb_Engines2_SelectionChanged;

	}

	private void Cb_ContextMenu_Checked(object sender, RoutedEventArgs e)
	{
		if (!e.IsLoaded()) {
			return;
		}

		AppUtil.HandleContextMenu(!AppUtil.IsContextMenuAdded);
		e.Handled = true;

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

		BaseUploadEngine.Default = LitterboxEngine.Instance;
	}

	#endregion

	#region

	private void Wnd_Main_Loaded(object sender, RoutedEventArgs e)
	{
		if (UseClipboard) {
			m_cbDispatch.Start();
		}

		m_trDispatch.Start();
		e.Handled = true;
	}

	private void Wnd_Main_Unloaded(object sender, RoutedEventArgs e)
	{
		e.Handled = true;

	}

	private void Wnd_Main_Closed(object sender, EventArgs e) { }

	private void Wnd_Main_Closing(object sender, CancelEventArgs e) { }

	#endregion

	#endregion

	#region

	private void OpenItem_Click(object sender, RoutedEventArgs e)
	{
		CurrentResultItem.Open();
	}

	private void DownloadItem_Click(object sender, RoutedEventArgs e)
	{
		if (CurrentResultItem is UniResultItem uri) {
			Application.Current.Dispatcher.InvokeAsync(() => DownloadResultAsync(uri));

		}

		e.Handled = true;
	}

	private void ScanItem_Click(object sender, RoutedEventArgs e)
	{
		Application.Current.Dispatcher.InvokeAsync(() => ScanResultAsync(CurrentResultItem));
		e.Handled = true;
	}

	#endregion

	private void Img_Preview_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (e.IsDoubleClick()) { }
	}

	private void Domain_UHException(object sender, UnhandledExceptionEventArgs e)
	{
		Log(new LogEntry($"AppDomain: {((Exception) e.ExceptionObject).Message}"));
	}

	private void Dispatcher_UHException(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		Log(new LogEntry($"Dispatcher: {e.Exception.Message}"));
		e.Handled = true;
	}
}