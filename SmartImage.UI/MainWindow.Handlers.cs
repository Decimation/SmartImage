// Read S SmartImage.UI MainWindow.Handlers.cs
// 2023-07-23 @ 11:50 AM

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Flurl;
using Kantan.Net.Utilities;
using Novus.OS;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Impl.Upload;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartImage.UI;

public partial class MainWindow
{
	#region

	#region

	private async void Tb_Input_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (Interlocked.CompareExchange(ref _status, S_OK, S_NO) == S_NO) {
			return;
		}

		var txt = InputText;
		var ok  = SearchQuery.IsValidSourceType(txt);

		if (ok /*&& !IsInputReady()*/) {
			Application.Current.Dispatcher.InvokeAsync(async () =>
			{
				await SetQueryAsync(txt);

			});
		}

		Btn_Run.IsEnabled = ok;
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
		InputText = f1;
		e.Handled = true;

	}

	private void Tb_Upload_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		HttpUtilities.TryOpenUrl(Query.Upload);
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

	private async void Lv_Queue_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.AddedItems.Count > 0) {
			Restart();
			var i = e.AddedItems[0] as string;
			InputText = i;
			// Next(i);
		}
	}

	private void Lv_Queue_KeyDown(object sender, KeyEventArgs e) { }

	#endregion

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

	private void Btn_Run_Click(object sender, RoutedEventArgs e)
	{
		// await SetQueryAsync(InputText);
		Btn_Run.IsEnabled = false;

		Application.Current.Dispatcher.InvokeAsync(RunAsync);
	}

	private async void Btn_Clear_Click(object sender, RoutedEventArgs e)
	{
		Clear();
	}

	private void Btn_Restart_Click(object sender, RoutedEventArgs e)
	{
		var ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
		Restart(ctrl);
		Queue.Clear();
	}

	private async void Btn_Next_Click(object sender, RoutedEventArgs e)
	{
		Next();
	}

	private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
	{
		Cancel();
	}

	private void Btn_Run_Loaded(object sender, RoutedEventArgs e)
	{
		// Btn_Run.IsEnabled = false;
	}

	#region

	private void Lv_Results_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		if (Lv_Results.SelectedItem is ResultItem si) {
			HttpUtilities.TryOpenUrl(si.Result.Url);
		}
	}

	private void Lv_Results_MouseRightButtonDown(object sender, MouseButtonEventArgs e)  { }
	private void Lv_Results_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

	private void Lv_Results_KeyDown(object sender, KeyEventArgs e)
	{

		var ctrl  = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
		var alt   = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);
		var shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

		var key = e.Key;

		switch (key) {
			case Key.D when ctrl:
				Application.Current.Dispatcher.InvokeAsync(async () =>
				{
					var    ri = ((ResultItem) Lv_Results.SelectedItem);
					var    u  = ri.Uni;
					var    v  = (Url) u.Value.ToString();
					string path;

					if (v.PathSegments is { Count: >= 1 }) {
						path = $"{v.PathSegments[^1]}";

					}
					else path = v.Path;

					path = HttpUtility.HtmlDecode(path);
					path = FileSystem.SanitizeFilename(path);
					var path2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), path);

					var f = File.OpenWrite(path2);

					if (u.Stream.CanSeek) {
						u.Stream.Position = 0;

					}

					await u.Stream.CopyToAsync(f);
					FileSystem.ExploreFile(path2);
					f.Dispose();
					// u.Dispose();
				});

				break;
			case Key.S when ctrl:

				Application.Current.Dispatcher.InvokeAsync(async () =>
				{
					var ri = ((ResultItem) Lv_Results.SelectedItem);

					if (m_uni.ContainsKey(ri)) {
						return;
					}

					Pb_Status.IsIndeterminate = true;
					var d = await ri.Result.LoadUniAsync();

					if (d) {
						Debug.WriteLine($"{ri}");
						var resultUni = ri.Result.Uni;
						m_uni.TryAdd(ri, resultUni);
						var resultItems = new ResultItem[resultUni.Length];

						for (int i = 0; i < resultUni.Length; i++) {
							var rii = new ResultItem(ri.Result, $"{ri.Name} {i} 🖼", ri.Status, idx: i);
							resultItems[i] = rii;
							Results.Insert(Results.IndexOf(ri) + 1 + i, rii);
						}
					}

					Pb_Status.IsIndeterminate = false;

				});

				break;
		}
	}

	#endregion

	#region

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

	#endregion

	#region

	private void Wnd_Main_Loaded(object sender, RoutedEventArgs e)
	{
		m_cbDispatch.Start();

	}

	private void Wnd_Main_Closed(object sender, EventArgs e)        { }
	private void Wnd_Main_Closing(object sender, CancelEventArgs e) { }

	#endregion

	private void Rb_UploadEngine_Catbox_Checked(object sender, RoutedEventArgs e)
	{
		if (!e.IsLoaded()) {
			return;
		}

		BaseUploadEngine.Default = CatboxEngine.Instance;
	}

	private void Rb_UploadEngine_Litterbox_Checked(object sender, RoutedEventArgs e)
	{
		if (!e.IsLoaded()) {
			return;
		}

		BaseUploadEngine.Default = LitterboxEngine.Instance;
	}

	#endregion
}