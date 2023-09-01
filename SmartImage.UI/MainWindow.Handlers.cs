// Read S SmartImage.UI MainWindow.Handlers.cs
// 2023-07-23 @ 11:50 AM

global using VBFS = Microsoft.VisualBasic.FileIO.FileSystem;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Kantan.Text;
using Kantan.Utilities;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Impl.Search;
using SmartImage.Lib.Engines.Impl.Upload;
using SmartImage.Lib.Utilities;
using SmartImage.UI.Model;
using static System.Net.Mime.MediaTypeNames;
using Application = System.Windows.Application;
using FileSystem = Novus.OS.FileSystem;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartImage.UI;

public partial class MainWindow
{
	#region

	#region

	/*private void Tb_Input_TextChanged(object sender, TextChangedEventArgs e)
	{
		var nt  = Tb_Input.Text;
		var txt = nt;
		var ok  = SearchQuery.IsValidSourceType(txt);

		// CurrentQueueItem = txt;

		// Debug.Assert(txt == CurrentQueueItem);
		if (ok /*&& !IsInputReady()#1#) {
			Application.Current.Dispatcher.InvokeAsync(() => UpdateQueryAsync(CurrentQueueItem));
		}

		Btn_Run.IsEnabled = ok;
		e.Handled         = true;
	}

	private void Tb_Input_TextInput(object sender, TextCompositionEventArgs e)
	{
		e.Handled = true;
	}*/

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

		AddToQueue(files1);
		var f1 = files1.FirstOrDefault();

		if (!string.IsNullOrWhiteSpace(f1)) {
			CurrentQueueItem = f1;

		}

		e.Handled = true;

	}

	private void Tb_Info_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		var s = Query.ValueString;

		if (string.IsNullOrWhiteSpace(s)) {
			return;
		}

		if (Query.Uni.IsFile) {
			FileSystem.ExploreFile(s);

			// FileSystem.Open(s);
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

	private void Lb_Queue_Drop(object sender, DragEventArgs e)
	{
		var files = e.GetFilesFromDrop().Where(SearchQuery.IsValidSourceType).ToArray();

		AddToQueue(files);
		e.Handled = true;
	}

	private void Lb_Queue_DragOver(object sender, DragEventArgs e)
	{
		e.Handled = true;
	}

	private void Lb_Queue_PreviewDragOver(object sender, DragEventArgs e)
	{
		e.Handled = true;
	}

	private void Lb_Queue_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.OriginalSource != sender) {
			return;
		}

		e.Handled = true;
	}

	private void Lb_Queue_KeyDown(object sender, KeyEventArgs e)
	{
		e.Handled = true;

	}

	#endregion

	private void Btn_Run_Click(object sender, RoutedEventArgs e)
	{
		// await SetQueryAsync(InputText);
		Btn_Run.IsEnabled = false;
		// Clear(true);
		// ReloadToken();
		ClearResults(true);
		Dispatcher.InvokeAsync(RunAsync);
		e.Handled = true;
	}

	private void Btn_Clear_Click(object sender, RoutedEventArgs e)
	{
		// var ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
		ClearResults(true);
		e.Handled = true;
	}

	private void Btn_Reset_Click(object sender, RoutedEventArgs e)
	{
		Reset();
		e.Handled = true;
	}

	private void Btn_Restart_Click(object sender, RoutedEventArgs e)
	{
		var ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
		Restart(ctrl);
		Queue.Clear();
		e.Handled = true;
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
		Lb_Queue.IsEnabled = true;
		e.Handled          = true;
	}

	private void Btn_Run_Loaded(object sender, RoutedEventArgs e)
	{
		// Btn_Run.IsEnabled = false;
		e.Handled = true;
	}

	private void Btn_Remove_Click(object sender, RoutedEventArgs e)
	{
		// var q   = MathHelper.Wrap(QueueSelectedIndex + 1, Queue.Count);

		var old = CurrentQueueItem;
		var i   = Queue.IndexOf(old);
		Queue.Remove(old);

		// TrySeekQueue(q);
		// AdvanceQueue(-1);

		if (m_queries.TryRemove(old, out var sq)) {
			foreach (var r in m_resultMap[sq]) {
				r.Dispose();
			}
			
			m_images.TryRemove(sq, out var img);
		}

		var i2 = i - 1;

		i2 = Math.Clamp(i2, 0, Queue.Count);
		string n;

		if (Queue.Count == 0)
			n = String.Empty;
		else
			n = Queue[i2];

		CurrentQueueItem = n;

		sq?.Dispose();
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		// AdvanceQueue();
		e.Handled = true;
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
		Btn_Remove.IsEnabled = !ok;
		AdvanceQueue();
		// FileSystem.SendFileToRecycleBin(InputText);
		e.Handled = true;
	}

	#region

	private void Lv_Results_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		CurrentResultItem?.Open();
		e.Handled = true;
	}

	private void Lv_Results_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;

	}

	private void Lv_Results_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.AddedItems.Count > 0) {

			var ai = e.AddedItems[0];
			var ri = ai as ResultItem;

			if (ri is UniResultItem uri) {
				UpdatePreview(uri);
			}
			else if (ri.Result.Root.Engine.EngineOption != SearchEngineOptions.TraceMoe){
				UpdatePreview(ri);
			}
			else {
				UpdatePreview(m_image);
				if (ri.Result.Metadata is TraceMoeEngine.TraceMoeDoc doc) {
					Br_Preview.Visibility       = Visibility.Hidden;
					Img_Preview.Visibility      = Visibility.Hidden;
					Br_Preview2.Visibility      = Visibility.Visible;
					Me_Preview.Visibility       = Visibility.Visible;
					Me_Preview.ScrubbingEnabled = false;
					Me_Preview.UnloadedBehavior = MediaState.Close;
					Me_Preview.LoadedBehavior   = MediaState.Manual;
					Me_Preview.Source           = new Uri(doc.video, UriKind.Absolute);
					Me_Preview.Play();
					m_me = true;
				}
				else {
					if (m_me) {
						Br_Preview.Visibility  = Visibility.Visible;
						Img_Preview.Visibility = Visibility.Visible;
						Br_Preview2.Visibility = Visibility.Hidden;
						Me_Preview.Visibility  = Visibility.Hidden;
						Me_Preview.Stop();
						Me_Preview.Close();
						Me_Preview.Source = null;
						m_me              = false;
					}
				}

			}

			ChangeStatus2(ri);

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
				Dispatcher.InvokeAsync(
					() => DownloadResultAsync((UniResultItem) CurrentResultItem));

				break;
			case Key.S when ctrl:

				Dispatcher.InvokeAsync(() => ScanResultAsync(CurrentResultItem));
				break;
			case Key.Delete:
				if (CurrentResultItem == null) {
					return;
				}

				if (CurrentResultItem is UniResultItem uri && m_uni.TryRemove(uri, out var x)) { }

				CurrentResultItem.Dispose();

				Results.Remove(CurrentResultItem);

				Img_Preview.Source = m_image;
				break;
			case Key.C when ctrl:
				Dispatcher.InvokeAsync(() =>
				{
					var text = CurrentResultItem.Url;
					m_clipboard.Add(text);
					Clipboard.SetText(text);
				});
				break;
			case Key.G when ctrl:
				Dispatcher.InvokeAsync(() => ScanGalleryResultAsync(CurrentResultItem));

				break;
			case Key.F when ctrl:
				// TODO: WIP
				Dispatcher.InvokeAsync(FilterResultsAsync);

				break;
			case Key.I when ctrl:
				OpenResultWindow(CurrentResultItem);
				break;
			case Key.Tab when ctrl:
				int i = shift ? -1 : 1;
				AdvanceQueue(i);
				break;
			case Key.E when ctrl:
				Dispatcher.InvokeAsync(() => EnqueueResultAsync((UniResultItem) CurrentResultItem));

				break;
			case Key.R when ctrl && alt:
				Dispatcher.InvokeAsync(() => RetryEngineAsync(CurrentResultItem));
				break;
		}

		e.Handled = true;
	}

	#endregion

	#region

	private async void Lb_Engines_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		Lb_Engines.SelectionChanged -= Lb_Engines_SelectionChanged;

		var n = Lb_Engines.HandleEnum(e, Config.SearchEngines);

		Lb_Engines.HandleEnum(n);
		Config.SearchEngines = n;
		await Client.ApplyConfigAsync();

		e.Handled                   =  true;
		Lb_Engines.SelectionChanged += Lb_Engines_SelectionChanged;
	}

	private async void Lb_Engines2_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		Lb_Engines2.SelectionChanged -= Lb_Engines2_SelectionChanged;

		var n = Lb_Engines2.HandleEnum(e, Config.PriorityEngines);

		Lb_Engines2.HandleEnum(n);
		Config.PriorityEngines = n;
		await Client.ApplyConfigAsync();

		e.Handled                    =  true;
		Lb_Engines2.SelectionChanged += Lb_Engines2_SelectionChanged;

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

	private async void Tb_EhUsername_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (!e.IsLoaded()) {
			return;
		}

		await Client.ApplyConfigAsync();
		e.Handled = true;
	}

	private async void Tb_EhPassword_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (!e.IsLoaded()) {
			return;
		}

		await Client.ApplyConfigAsync();
		e.Handled = true;

	}

	private void Tb_HyEndpoint_OnTextChanged(object sender, TextChangedEventArgs e)
	{
		if (!e.IsLoaded()) {
			return;
		}

		e.Handled = true;
	}

	private void Tb_HyKey_OnTextChanged(object sender, TextChangedEventArgs e)
	{
		if (!e.IsLoaded()) {
			return;
		}

		e.Handled = true;
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
		Debug.WriteLine("Main loaded");

	}

	private void Wnd_Main_Unloaded(object sender, RoutedEventArgs e)
	{
		Debug.WriteLine("Main unloaded");
		e.Handled = true;

	}

	private void Wnd_Main_Closed(object sender, EventArgs e)
	{
		Debug.WriteLine("Main closed");

	}

	private void Wnd_Main_Closing(object sender, CancelEventArgs e)
	{
		Debug.WriteLine("Main closing");

	}

	private void Wnd_Main_KeyDown(object sender, KeyEventArgs e)
	{

		// e.Handled = true;

	}

	#endregion

	#endregion

	#region

	private void OpenItem_Click(object sender, RoutedEventArgs e)
	{
		CurrentResultItem.Open();
		e.Handled = true;
	}

	private void DownloadItem_Click(object sender, RoutedEventArgs e)
	{
		if (CurrentResultItem is UniResultItem uri) {
			Dispatcher.InvokeAsync(() => DownloadResultAsync(uri));

		}

		e.Handled = true;
	}

	private void ScanItem_Click(object sender, RoutedEventArgs e)
	{
		Dispatcher.InvokeAsync(() => ScanResultAsync(CurrentResultItem));
		e.Handled = true;
	}

	private void InfoItem_Click(object sender, RoutedEventArgs e)
	{
		OpenResultWindow(CurrentResultItem);
		e.Handled = true;

	}

	private void RetryItem_Click(object sender, RoutedEventArgs e)
	{
		Dispatcher.InvokeAsync(() => RetryEngineAsync(CurrentResultItem));
		e.Handled = true;
	}

	private void EnqueueItem_Click(object sender, RoutedEventArgs e)
	{
		Dispatcher.InvokeAsync(() => EnqueueResultAsync((UniResultItem) CurrentResultItem));
		e.Handled = true;
	}

	#endregion

	private void Img_Preview_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (e.IsDoubleClick()) {
			// Img_Preview.Width  = Img_Preview.Source.Width;
			// Img_Preview.Height = Img_Preview.Source.Height;

			/*if (this.m_popup == null) {
				this.m_popup                    = new PopupWindow();
				this.m_popup.Img_Preview.Source = m_image;

				this.m_popup.Closed += (sender, args) =>
				{
					this.m_popup.Img_Preview.Source = null;
					this.m_popup = null;
				};
				this.m_popup.Show();
			}*/
			FileSystem.Open(CurrentQueueItem);
		}

		e.Handled = true;
	}

	private void Me_Preview_MouseDown(object sender, MouseButtonEventArgs e)
	{

		e.Handled = true;
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

	private void Btn_OpenFolder_Click(object sender, RoutedEventArgs e)
	{
		FileSystem.Open(AppUtil.CurrentAppFolder);
		e.Handled = true;
	}

	private void Btn_OpenWiki_Click(object sender, RoutedEventArgs e)
	{
		FileSystem.Open(R1.Wiki_Url);
		e.Handled = true;
	}

	private void Btn_Browse_Click(object sender, RoutedEventArgs e)
	{
		var ofn = new OpenFileDialog
		{
			Multiselect = true,
			Filter      = $"Image files|{ImageHelper.Ext.QuickJoin(";")}"
		};

		var d = ofn.ShowDialog(this);

		if (d.HasValue && d.Value) {
			var fn = ofn.FileNames;
			AddToQueue(fn);
		}

		e.Handled = true;
	}

	private void Ti_Main_KeyDown(object sender, KeyEventArgs e)
	{
		// e.Handled = true;
	}

	private void Btn_Filter_Click(object sender, RoutedEventArgs e)
	{
		Dispatcher.InvokeAsync(FilterResultsAsync);
		e.Handled = true;
	}

	private void Tb_Search_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (string.IsNullOrWhiteSpace(Tb_Search.Text) && m_resultMap.TryGetValue(Query, out var value)) {
			Lv_Results.ItemsSource = value;
		}
		else {
			var selected = (string) Cb_SearchFields.SelectionBoxItem;
			var strFunc  = SearchFields[selected];

			var searchResults = Results.Where(r =>
			{
				var s = strFunc(r);

				if (string.IsNullOrWhiteSpace(s)) {
					return false;
				}

				return s.Contains(Tb_Search.Text, StringComparison.InvariantCultureIgnoreCase);
			});
			Lv_Results.ItemsSource = searchResults;

		}

		e.Handled = true;
	}
}