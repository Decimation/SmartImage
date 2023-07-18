using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using AngleSharp.Dom;
using Kantan.Net.Utilities;
using Kantan.Utilities;
using Microsoft.Extensions.Logging;
using Novus.FileTypes;
using Novus.Win32;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;
using static System.Net.Mime.MediaTypeNames;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;
using Timer = System.Timers.Timer;
using Url = Flurl.Url;

namespace SmartImage.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, IDisposable
{
	public MainWindow()
	{
		InitializeComponent();

		DataContext = this;
		Client      = new SearchClient(new SearchConfig());
		Results     = new();

		Query      = SearchQuery.Null;
		Queue      = new();
		m_queuePos = 0;
		m_cts      = new CancellationTokenSource();
		Engines1   = new(Engines);
		Engines2   = new(Engines);

		Lv_Results.ItemsSource = Results;
		Lv_Queue.ItemsSource   = Queue;

		Client.OnResult += OnResult;

		Lb_Engines.ItemsSource = Engines1;
		Lb_Engines.SelectAll();
		Lb_Engines2.ItemsSource = Engines2;

		m_auto = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(1)
		};
		m_auto.Tick += Start;
		m_uni       =  new();
		m_clipboard =  new();

		BindingOperations.EnableCollectionSynchronization(Results, m_lock);
	}

	#region

	private static readonly ILogger Logger = LogUtil.Factory.CreateLogger(nameof(MainWindow));

	public static SearchEngineOptions[] Engines { get; } = Enum.GetValues<SearchEngineOptions>();

	#endregion

	#region

	private readonly object m_lock = new();

	public List<SearchEngineOptions> Engines1 { get; }

	public List<SearchEngineOptions> Engines2 { get; }

	public SearchClient Client { get; }

	public SearchConfig Config => Client.Config;

	public SearchQuery Query { get; internal set; }

	public ObservableCollection<ResultItem> Results { get; }

	public ObservableCollection<string> Queue { get; }

	#endregion

	#region

	private CancellationTokenSource m_cts;

	private readonly ConcurrentDictionary<ResultItem, UniSource[]> m_uni;
	private readonly List<string>                                  m_clipboard;
	private readonly DispatcherTimer                               m_auto;

	private int m_queuePos;

	#endregion

	#region

	private async void Btn_Run_Click(object sender, RoutedEventArgs e)
	{
		// await SetQueryAsync(Tb_Input.Text);
		Btn_Run.IsEnabled = false;

		var r = await Client.RunSearchAsync(Query, token: m_cts.Token);

	}

	private async void Btn_Clear_Click(object sender, RoutedEventArgs e)
	{
		Clear();
	}

	private async void Tb_Input_TextChanged(object sender, TextChangedEventArgs e)
	{
		var txt = Tb_Input.Text;
		var ok  = SearchQuery.IsValidSourceType(txt);

		if (ok) {
			await SetQueryAsync(txt);
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

	private void Tb_Input2_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		HttpUtilities.TryOpenUrl(Query.Upload);
	}

	private void Tb_Input_Drop(object sender, DragEventArgs e)
	{
		var files1 = GetFilesFromDrop(e);

		Enqueue(files1);
		var files = files1;
		var f1    = files.FirstOrDefault();
		Tb_Input.Text = f1;
		e.Handled     = true;

	}

	private void Lv_Queue_Drop(object sender, DragEventArgs e)
	{
		var files = GetFilesFromDrop(e);

		Enqueue(files);
		string[] temp = files;
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

	private void Lb_Engines_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		var rg = Lb_Engines.Items.OfType<SearchEngineOptions>().ToArray();

		var ai = e.AddedItems.OfType<SearchEngineOptions>()
			.Aggregate(default(SearchEngineOptions), (n, l) => n | l);

		var ri = e.RemovedItems.OfType<SearchEngineOptions>()
			.Aggregate(default(SearchEngineOptions), (n, l) => n | l);

		var si = Lb_Engines.SelectedItems.OfType<SearchEngineOptions>().ToArray();

		if (ai.HasFlag(SearchEngineOptions.All)) {
			Lb_Engines.SelectAll();
		}

		if (ri.HasFlag(SearchEngineOptions.All)) {
			Lb_Engines.UnselectAll();
		}

		if (ai.Equals(SearchEngineOptions.Artwork)) {
			var sf1 = EnumHelper.GetSetFlags(SearchEngineOptions.Artwork);

			foreach (var seo in sf1) {
				Lb_Engines.SelectedItems.Add(seo);
			}
		}

		if (ri.Equals(SearchEngineOptions.Artwork)) {
			var sf1 = EnumHelper.GetSetFlags(SearchEngineOptions.Artwork);

			foreach (var seo in sf1) {
				Lb_Engines.SelectedItems.Remove(seo);
			}
		}

		for (int i = 0; i < rg.Length; i++) { }

		Config.SearchEngines |= (ai);
		Config.SearchEngines &= ~ri;

		/*for (int i = 0; i < Lb_Engines.SelectedItems.Count; i++) {
			var ix = (SearchEngineOptions) Lb_Engines.SelectedItems[i];

			if (!Config.SearchEngines.HasFlag(ix)) {
				Lb_Engines.SelectedItems.Remove(ix);
			}
		}*/
		Debug.WriteLine($"{ai} {si} -> {Config.SearchEngines}");

		var sf = EnumHelper.GetSetFlags(Config.SearchEngines, false);

	}

	private void Lb_Engines2_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

	private void Btn_Restart_Click(object sender, RoutedEventArgs e)
	{
		Restart();

	}

	private async void Btn_Next_Click(object sender, RoutedEventArgs e)
	{
		Restart();

		if (m_queuePos < Queue.Count && m_queuePos >= 0) {
			var next = Queue[m_queuePos++];
			Tb_Input.Text = next;
			await SetQueryAsync(next);
		}
	}

	private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
	{
		m_cts.Cancel();
	}

	private void Lv_Results_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		if (Lv_Results.SelectedItem is ResultItem si) {
			HttpUtilities.TryOpenUrl(si.Result.Url);
		}
	}

	private void Btn_Run_Loaded(object sender, RoutedEventArgs e)
	{
		// Btn_Run.IsEnabled = false;
	}

	private void Lv_Results_MouseRightButtonDown(object sender, MouseButtonEventArgs e) { }

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
					var ri = ((ResultItem) Lv_Results.SelectedItem);

					if (m_uni.TryGetValue(ri, out var us)) {

					}
				});

				break;
			case Key.S when ctrl:

				Application.Current.Dispatcher.InvokeAsync(async () =>
				{
					var ri = ((ResultItem) Lv_Results.SelectedItem);

					if (m_uni.ContainsKey(ri)) {
						return;
					}

					var d = await ri.Result.LoadUniAsync();

					if (d) {
						Debug.WriteLine($"{ri}");
						var resultUni = ri.Result.Uni;
						m_uni.TryAdd(ri, resultUni);
						var resultItems = new ResultItem[resultUni.Length];

						for (int i = 0; i < resultUni.Length; i++) {
							var rii = new ResultItem(null, $"{resultUni[i].SourceType} (DI)", resultUni[i].Value.ToString());
							resultItems[i] = rii;
							Results.Insert(Results.IndexOf(ri) + 1 + i, rii);
						}
					}
				});

				break;
			default:
				break;
		}
	}

	private void Wnd_Main_Loaded(object sender, RoutedEventArgs e)
	{
		m_auto.Start();

	}

	#endregion

	#region

	private bool IsInputReady()
	{
		return !string.IsNullOrWhiteSpace(Tb_Input.Text);
	}

	private async void Start(object? s, EventArgs e)
	{
		var cImg  = Clipboard.ContainsImage();
		var cText = Clipboard.ContainsText();
		var cFile = Clipboard.ContainsFileDropList();

		if (cImg) {
			var bmp = (Bitmap) Clipboard.GetData(DataFormats.Bitmap);
		}

		if (cText) {
			var txt = (string) Clipboard.GetData(DataFormats.Text);

			if (SearchQuery.IsValidSourceType(txt)) {

				if (!IsInputReady() && !m_clipboard.Contains(txt)) {
					m_clipboard.Add(txt);
					await SetQueryAsync(txt);
				}
			}

			return;
		}

		if (cFile) {
			var files = Clipboard.GetFileDropList();
			var rg    = new string[files.Count];
			files.CopyTo(rg, 0);
			rg = rg.Where(x => !m_clipboard.Contains(x)).ToArray();
			Enqueue(rg);

			return;
		}

		// Thread.Sleep(1000);
	}

	private async Task SetQueryAsync(string q)
	{
		Btn_Run.IsEnabled         = false;

		Query                     = await SearchQuery.TryCreateAsync(q);
		Pb_Status.IsIndeterminate = true;
		var b = Query != SearchQuery.Null;

		if (b) {
			var u = await Query.UploadAsync();
			Tb_Input2.Text            = u;
			Pb_Status.IsIndeterminate = false;
		}
		else { }

		Btn_Run.IsEnabled = b;
	}

	private void OnResult(object o, SearchResult result)
	{
		Pb_Status.Value += (Results.Count / (double) Client.Engines.Length) * 10;

		lock (m_lock) {
			int i = 0;

			var allResults = result.AllResults;

			var sri1 = new SearchResultItem(result)
			{
				Url = result.RawUrl,
			};

			Results.Add(new ResultItem(sri1, $"{sri1.Root.Engine.Name} (Raw)"));

			foreach (SearchResultItem sri in allResults) {
				Results.Add(new ResultItem(sri, $"{sri.Root.Engine.Name} #{++i}"));

			}
		}
	}

	private async void Enqueue(string[] files)
	{
		if (!IsInputReady()) {
			var ff = files[0];
			Tb_Input.Text = ff;

			if (files.Length > 1) {
				files = files[1..];

			}

			await SetQueryAsync(ff);
		}

		foreach (var s in files) {

			if (!Queue.Contains(s)) {
				Queue.Add(s);
			}
		}
	}

	private static string[] GetFilesFromDrop(DragEventArgs e)
	{
		if (e.Data.GetDataPresent(DataFormats.FileDrop)) {

			if (e.Data.GetData(DataFormats.FileDrop, true) is string[] files
			    && files.Any()) {

				return files;

			}
		}

		return Array.Empty<string>();
	}

	private void Restart()
	{
		Clear();
		Dispose(false);
		m_cts = new();
		m_clipboard.Clear();
	}

	private void Clear()
	{
		Results.Clear();
		Btn_Run.IsEnabled = false;
		Tb_Input.Text     = string.Empty;
		Query.Dispose();
		Pb_Status.Value = 0;
	}

	public void Dispose(bool full)
	{
		if (full) {
			Client.Dispose();
			Query.Dispose();

			Queue.Clear();
			m_queuePos = 0;
		}

		foreach (var r1 in Results) {
			r1.Dispose();
		}

		Results.Clear();
		m_cts.Dispose();
	}

	public void Dispose()
	{
		Dispose(true);
	}

	#endregion
}