// Read S SmartImage.UI MainWindow.Handlers.cs
// 2023-07-23 @ 11:50 AM

global using VBFS = Microsoft.VisualBasic.FileIO.FileSystem;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Flurl.Http;
using Kantan.Net.Utilities;
using Kantan.Text;
using Kantan.Utilities;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Impl.Search;
using SmartImage.Lib.Engines.Impl.Upload;
using SmartImage.Lib.Model;
using SmartImage.Lib.Utilities;
using SmartImage.UI.Model;
using static System.Net.Mime.MediaTypeNames;
using Application = System.Windows.Application;
using FileSystem = Novus.OS.FileSystem;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartImage.UI;
#nullable disable
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
		var files = e.GetFilesFromDrop();

		if (files.All(x => !SearchQuery.IsValidSourceType(x))) {
			e.Effects = DragDropEffects.None;
		}

		e.Handled = true;

	}

	private void Tb_Input_Drop(object sender, DragEventArgs e)
	{
		var files1 = e.GetFilesFromDrop();

		AddToQueue(files1);
		var f1 = files1.FirstOrDefault();

		if (!String.IsNullOrWhiteSpace(f1)) {
			SetQueue(f1, out _);

		}

		e.Handled = true;

	}

	private void Tb_Input_OnTextChanged(object sender, TextChangedEventArgs e)
	{
		/*if (!m_isq) {
			OnCurrentQueueItemChanged(sender, null);
		}*/
		if (SearchQuery.IsValidSourceType(Input) && Queue.All(x => x.Value != Input)) {
			var q = new QueryModel(Input);
			Queue.Add(q);
			CurrentQuery = q;
		}

		/*if (QueueItemSelected) {
			CurrentQueueItem.Value = Input;

		}*/
		/*if (QueueItemSelected) {
			CurrentQueueItem.Value = Input;

		}*/

		/*if (SearchQuery.IsValidSourceType(CurrentQueueItem?.Value)) {
			// Queue.Insert(0, new QueryModel());
			// Lb_Queue.SelectedIndex = 1;
			// OnCurrentQueueItemChanged(sender, null);

		}*/
		e.Handled = true;
	}

	private void Tb_Info_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		if (Query is not { }) {
			return;
		}

		var s = Query.ValueString;

		if (String.IsNullOrWhiteSpace(s)) {
			return;
		}

		if (Query.IsFile) {
			FileSystem.ExploreFile(s);

			// FileSystem.Open(s);
		}
		else if (Query.IsUri) {
			FileSystem.Open(s);

		}
	}

	private void Tb_Upload_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		if (Query is not { }) {
			return;
		}

		FileSystem.Open(Query.Upload);
	}

	#endregion

	#region

	private void Lb_Queue_Drop(object sender, DragEventArgs e)
	{
		var files = e.GetFilesFromDrop();

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
		Debug.WriteLine($"lqs: {sender} {e}");

		if (e.OriginalSource != sender) {
			return;
		}

		CheckMedia();

		e.Handled = true;
	}

	private void Lb_Queue_KeyDown(object sender, KeyEventArgs e)
	{
		e.Handled = true;

	}

	#endregion

	private void Btn_Run_Click(object sender, RoutedEventArgs e)
	{
		/*
		if (Query is not { IsUploaded: true }) {
			return;
		}
		*/

		// await SetQueryAsync(InputText);
		// Btn_Run.IsEnabled = false;
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

	private void Btn_Reload_Click(object sender, RoutedEventArgs e)
	{
		Cancel();
		ClearResults(false);

		// Query.Dispose();
		var cpy = CurrentQuery;
		var i   = Queue.IndexOf(cpy);
		Queue.Remove(cpy);
		cpy.Dispose();
		var rm = new QueryModel(cpy.Value);
		Queue.Insert(i, rm);
		CurrentQuery = rm;

		// ClearQueue();
		// ClearResults(true);
		ReloadToken();
		e.Handled = true;
	}

	private void Btn_Reload_MouseEnter(object sender, MouseEventArgs e)
	{
		e.Handled = true;
	}

	private void Btn_Reload_MouseLeave(object sender, MouseEventArgs e)
	{
		e.Handled = true;
	}

	private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
	{
		Cancel();
		ReloadToken();
		Lb_Queue.IsEnabled   = true;
		Btn_Run.IsEnabled    = true;
		Btn_Remove.IsEnabled = true;

		// m_us.Release();
		e.Handled = true;
	}

	private void Btn_Run_Loaded(object sender, RoutedEventArgs e)
	{
		// Btn_Run.IsEnabled = false;
		e.Handled = true;
	}

	private void Btn_Remove_Click(object sender, RoutedEventArgs e)
	{
		// var q   = MathHelper.Wrap(QueueSelectedIndex + 1, Queue.Count);
		var old = CurrentQuery;

		if (RemoveFromQueue(old))
			goto ret;

		// SetQueue(string.Empty);

		// TrySeekQueue(q);
		// AdvanceQueue(-1);

		/*if (m_queries.TryRemove(old, out var sq)) {
			m_resultMap.TryRemove(sq, out var result);

			foreach (var r in result) {
				r.Dispose();
			}
			result.Clear();
			/*foreach (var r in m_resultMap[sq]) {
				r.Dispose();
			}#1#

			m_images.TryRemove(sq, out var img);
			img = null;
		}*/

		// ClearQueryControls();

		/*var i2 = i - 1;

		i2 = Math.Clamp(i2, 0, Queue.Count);
		ResultModel n;

		if (Queue.Count == 0)
			n = null;
		else
			n = Queue[i2];
		// SetQueue(n);

		CurrentQueueItem = n;*/
		// Lb_Queue.ItemsSource.  = n;
		// AdvanceQueue(i-1);

		// n?.Dispose();

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		// AdvanceQueue();
	ret:
		e.Handled = true;
	}

	private void Btn_Delete_Click(object sender, RoutedEventArgs e)
	{
		Cancel();
		ClearResults();
		m_cbDispatch.Stop();
		var old = CurrentQuery;
		m_clipboardHistory.Remove(old.Value);
		old.Dispose();

		// CurrentQueueItem = null;
		// m_queries.TryRemove(old, out var q);
		// m_resultMap.TryRemove(Query, out var x);
		// Query.Dispose();
		Queue.Remove(old);
		Img_Preview.Source = Image = null;

		// Query              = SearchQuery.Null;
		bool ok;

		try {
			VBFS.DeleteFile(old.Value, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);

			// FileSystem.SendFileToRecycleBin(old);
			ok = true;
		}
		catch (Exception exception) {
			Debug.WriteLine($"{exception}");
			ok = false;
		}

		m_cbDispatch.Start();
		Btn_Delete.IsEnabled = ok;
		Btn_Remove.IsEnabled = ok;
		ReloadToken();

		// AdvanceQueue();
		// FileSystem.SendFileToRecycleBin(InputText);
		e.Handled = true;
	}

	#region

	private void Lv_Results_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		CurrentResult?.Open();
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

			var b = ri is UniResultItem uri;

			if (b || (ri.Result.Root.Engine.EngineOption != SearchEngineOptions.TraceMoe)) {
				if (b) {
					ri = (UniResultItem) ri;
				}

				SetPreview(ri);
				CheckMedia();
			}
			else {
				SetPreviewToCurrentQuery();

				if (ri.Result.Metadata is TraceMoeEngine.TraceMoeDoc doc) {
					/*Me_Preview.ScrubbingEnabled = true;
Me_Preview.UnloadedBehavior = MediaState.Close;
Me_Preview.LoadedBehavior   = MediaState.Manual;*/
					// Me_Preview.UnloadedBehavior   = MediaState.Stop;
					// Me_Preview.LoadedBehavior   = MediaState.Manual;
					// Me_Preview                  = new MediaElement();
					// Me_Preview.LoadedBehavior   = MediaState.Play;
					// Me_Preview.UnloadedBehavior = MediaState.Manual;
					Me_Preview.UnloadedBehavior = MediaState.Close;
					Me_Preview.LoadedBehavior   = MediaState.Manual;

					// Me_Preview.LoadedBehavior   = MediaState.Manual;
					/*if (!await m_us2.WaitAsync(TimeSpan.Zero)) {
						return;

					}*/
					Tb_Preview.Text = $"Preview: loading {ri.Name}";

					Dispatcher.InvokeAsync(async () =>
					{
						/*Me_Preview.ScrubbingEnabled = true;
						Me_Preview.UnloadedBehavior = MediaState.Close;
						Me_Preview.LoadedBehavior   = MediaState.Manual;*/
						// Me_Preview.UnloadedBehavior   = MediaState.Stop;
						// Me_Preview.LoadedBehavior   = MediaState.Manual;
						// Me_Preview                  = new MediaElement();
						// Me_Preview.LoadedBehavior   = MediaState.Play;
						// Me_Preview.UnloadedBehavior = MediaState.Manual;
						Me_Preview.UnloadedBehavior = MediaState.Close;
						Me_Preview.LoadedBehavior   = MediaState.Manual;

						// Me_Preview.LoadedBehavior   = MediaState.Manual;
						/*if (!await m_us2.WaitAsync(TimeSpan.Zero)) {
							return;

						}*/
						Tb_Preview.Text = $"Preview: loading {ri.Name}";
						var uri = await CacheOrGetAsync(doc.video, m_ctsm.Token);
						Debug.WriteLine($"{m_ctsm.IsCancellationRequested}");

						if (uri != null) {
							Me_Preview.Source = new Uri(uri, UriKind.Absolute);

							// Me_Preview.Source = new Uri(doc.video);
							/*
						while (Me_Preview.BufferingProgress < 1f) {
							Debug.WriteLine($"{Me_Preview.BufferingProgress}");
						}
						*/
							Me_Preview.Play();

							ShowMedia       = true;
							Tb_Preview.Text = $"Preview: {ri.Name}";

							// m_us2.Release();

						}
					});

				}
				else {
					CheckMedia();
				}

			}

			ChangeStatus2(ri);

		}

		e.Handled = true;
		return;

	}

	private void Lv_Results_KeyDown(object sender, KeyEventArgs e)
	{
		(bool ctrl, bool alt, bool shift) = ControlsHelper.GetModifiers();

		var key = e.Key;

		switch (key) {
			case Key.D when ctrl:
				Dispatcher.InvokeAsync(
					() => DownloadResultAsync(CurrentResult));

				break;

			case Key.S when ctrl:

				Dispatcher.InvokeAsync(() => ScanResultAsync(CurrentResult));
				break;

			case Key.Delete:
				if (CurrentResult == null) {
					return;
				}

				if (CurrentResult is IDisposable uri && m_uni.TryRemove(uri, out var x)) { }

				CurrentResult.Dispose();

				// Results.Remove(CurrentResultItem);

				CurrentQuery.Results.Remove(CurrentResult);
				Img_Preview.Source = Image;
				break;

			case Key.C when ctrl:
				Dispatcher.InvokeAsync(() =>
				{
					var text = CurrentResult.Url;
					m_clipboardHistory.Add(text);
					Clipboard.SetText(text);
				});
				break;

			case Key.G when ctrl:
				Dispatcher.InvokeAsync(() => ScanGalleryResultAsync(CurrentResult));

				break;

			case Key.F when ctrl:
				// TODO: WIP
				Dispatcher.InvokeAsync(FilterResultsAsync);

				break;

			case Key.I when ctrl:
				OpenResultWindow(CurrentResult);
				break;

			case Key.Tab when ctrl:
				int i = shift ? -1 : 1;
				AdvanceQueue(i);
				break;

			case Key.E when ctrl:
				Dispatcher.InvokeAsync(() => EnqueueResultAsync(CurrentResult));

				break;

			case Key.R when ctrl && alt:
				Dispatcher.InvokeAsync(() => RetryEngineAsync(CurrentResult));
				break;
			/*
			case Key.H when ctrl:
				var w = new HydrusWindow(Shared);
				w.Show();
				break;
				*/

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

	private void Cm_UploadEngine_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		var text = (sender as ComboBox).SelectedItem as string;

		foreach (BaseUploadEngine engine in BaseUploadEngine.All) {
			if (engine.Name == text) {
				BaseUploadEngine.Default = engine;
				Debug.WriteLine($"Upload engine set to {engine}");
				break;

			}
		}

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
		SetRenderMode();

		if (UseClipboard) {
			m_cbDispatch.Start();
		}

		// todo: not used for now
		m_trDispatch.Start();
		Debug.WriteLine("Main loaded");
		e.Handled = true;

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

		foreach ((string key, string value) in FileCache) {
			Debug.WriteLine($"Deleting {key}={value}");
			File.Delete(value);
		}
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
		CurrentResult.Open();
		e.Handled = true;
	}

	private void DownloadItem_Click(object sender, RoutedEventArgs e)
	{
		if (CurrentResult is ResultItem { } uri && uri.Properties.HasFlag(ImageSourceProperties.CanDownload)) {
			Dispatcher.InvokeAsync(() => DownloadResultAsync(uri));

		}

		e.Handled = true;
	}

	private void ScanItem_Click(object sender, RoutedEventArgs e)
	{
		Dispatcher.InvokeAsync(() => ScanResultAsync(CurrentResult));
		e.Handled = true;
	}

	private void InfoItem_Click(object sender, RoutedEventArgs e)
	{
		OpenResultWindow(CurrentResult);
		e.Handled = true;

	}

	private void RetryItem_Click(object sender, RoutedEventArgs e)
	{
		Dispatcher.InvokeAsync(() => RetryEngineAsync(CurrentResult));
		e.Handled = true;
	}

	private void EnqueueItem_Click(object sender, RoutedEventArgs e)
	{
		Dispatcher.InvokeAsync(() => EnqueueResultAsync((UniResultItem) CurrentResult));
		e.Handled = true;
	}

	#endregion

	#region

	private void Img_Preview_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (!ShowMedia) {
			SetPreviewToCurrentQuery();
		}

		e.Handled = true;

	}

	private void Img_Preview_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (CurrentResult == null) {
			return;
		}

		SetPreview(CurrentResult);
		e.Handled = true;
	}

	private void Img_Preview_MouseDown(object sender, MouseButtonEventArgs e)
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
			var s = Img_Preview.Source.ToString();
			FileSystem.Open(s);
		}

		e.Handled = true;
	}

	#endregion

	private void Me_Preview_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
	}

	private void Me_Preview_MouseDown(object sender, MouseButtonEventArgs e)
	{
		PlayPauseMedia();

		// CloseMedia();
		// SetPreviewToCurrentQuery();
		e.Handled = true;

	}

#if !DEBUG
	private void Domain_UHException(object sender, UnhandledExceptionEventArgs e)
	{
		Log(new LogEntry($"AppDomain: {((Exception) e.ExceptionObject).Message}"));
	}

	private void Dispatcher_UHException(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		Log(new LogEntry($"Dispatcher: {e.Exception.Message}"));
		e.Handled = true;
	}
#endif
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
			Filter      = $"Image files|{SearchHelper.Ext.QuickJoin(";")}"
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

	private void Tb_SelPrevUrl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		if (CurrentResult.IsThumbnail) {
			FileSystem.Open(CurrentResult.Result.Thumbnail);

		}
	}

	private void Tb_SelUrl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{

		FileSystem.Open(CurrentResult.Url);
	}

	private void Tb_Search_TextChanged(object sender, TextChangedEventArgs e)
	{
		RunSearch(Tb_Search.Text);

		e.Handled = true;
	}

	private void MenuItem_OnClick(object sender, RoutedEventArgs e) { }

	private void OnValidationRaised(object sender, RoutedEventArgs e) { }

}