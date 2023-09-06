global using Clipboard2 = Novus.Win32.Clipboard;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Cache;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Flurl;
using JetBrains.Annotations;
using Kantan.Collections;
using Kantan.Diagnostics;
using Kantan.Net.Utilities;
using Kantan.Numeric;
using Kantan.Text;
using Kantan.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Novus.FileTypes;
using Novus.OS;
using Novus.Streams;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Impl.Upload;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;
using Windows.ApplicationModel;
using Flurl.Http;
using SmartImage.Lib.Model;
using SmartImage.UI.Model;
using Color = System.Drawing.Color;
using Jint.Parser.Ast;
using Novus.Win32;
using Novus.Win32.Structures.Kernel32;
using CancellationTokenSource = System.Threading.CancellationTokenSource;
using Clipboard = System.Windows.Clipboard;
using System.Data.Common;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime;
using ReactiveUI;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace SmartImage.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, IDisposable, INotifyPropertyChanged
{
	static MainWindow() { }

	public MainWindow()
	{
		Client    = new SearchClient(new SearchConfig());
		Shared    = new SharedInfo();
		m_queries = new ConcurrentDictionary<string, SearchQuery>();

		InitializeComponent();

		m_us        = new SemaphoreSlim(1);
		DataContext = this;
		SearchStart = default;
		Results     = new();

		Query = SearchQuery.Null;
		Queue = new() { };

		// QueueSelectedIndex = 0;
		_clipboardSequence = 0;
		m_cts              = new CancellationTokenSource();
		m_ctsu             = new CancellationTokenSource();

		Lb_Engines.ItemsSource  = Engines;
		Lb_Engines2.ItemsSource = Engines;
		Lb_Engines.HandleEnum(Config.SearchEngines);
		Lb_Engines2.HandleEnum(Config.PriorityEngines);

		Logs                   = new ObservableCollection<LogEntry>();
		Lv_Logs.ItemsSource    = Logs;
		Lv_Results.ItemsSource = Results;
		Lb_Queue.ItemsSource   = Queue;

		Client.OnResult   += OnResult;
		Client.OnComplete += OnComplete;

		AppDomain.CurrentDomain.UnhandledException       += Domain_UHException;
		Application.Current.DispatcherUnhandledException += Dispatcher_UHException;

		m_cbDispatch = new DispatcherTimer(DispatcherPriority.Background)
		{
			Interval = TimeSpan.FromSeconds(1)
		};
		m_cbDispatch.Tick += ClipboardListenAsync;

		m_trDispatch = new DispatcherTimer(DispatcherPriority.Background)
		{
			Interval = TimeSpan.FromSeconds(3)
		};
		m_trDispatch.Tick += IdleDispatchAsync;

		m_uni                    = new();
		m_clipboard              = new();
		Cb_ContextMenu.IsChecked = AppUtil.IsContextMenuAdded;
		m_resultMap              = new();
		m_image                  = null;

		Rb_UploadEngine_Catbox.IsChecked    = BaseUploadEngine.Default is CatboxEngine;
		Rb_UploadEngine_Litterbox.IsChecked = BaseUploadEngine.Default is LitterboxEngine;

		BindingOperations.EnableCollectionSynchronization(Queue, m_lock);
		BindingOperations.EnableCollectionSynchronization(Results, m_lock);
		RenderOptions.SetBitmapScalingMode(Img_Preview, BitmapScalingMode.HighQuality);

		Application.Current.Dispatcher.InvokeAsync(CheckForUpdate);

		// ResizeMode         = ResizeMode.NoResize; //todo

		m_pipeBuffer = new List<string>();

		((App) Application.Current).OnPipeMessage += OnPipeReceived;

		m_wndInterop = new WindowInteropHelper(this);

		Cb_SearchFields.ItemsSource   = SearchFields.Keys;
		Cb_SearchFields.SelectedIndex = 0;
		m_images                      = new();

		PropertyChangedEventManager.AddHandler(this, OnCurrentQueueItemChanged, nameof(CurrentQueueItem));
		// m_hydrus = new HydrusClient()
		ParseArgs(Args);

	}

	#region

	private static readonly ILogger Logger = LoggerFactory
		.Create(builder => builder.AddDebug().AddProvider(new DebugLoggerProvider()))
		.CreateLogger(nameof(MainWindow));

	public static SearchEngineOptions[] Engines { get; } = Enum.GetValues<SearchEngineOptions>();

	private readonly object m_lock = new();

	private readonly WindowInteropHelper m_wndInterop;

	private readonly SemaphoreSlim m_us;
	private readonly List<string>  m_pipeBuffer;

	#endregion

	#region

	public SearchClient Client { get; }

	public SearchConfig Config => Client.Config;

	public SearchQuery Query { get; internal set; }

	public ObservableCollection<ResultItem> Results { get; private set; }

	public bool UseContextMenu
	{
		get => AppUtil.IsContextMenuAdded;
		set => AppUtil.HandleContextMenu(value);
	}

	public bool InPath
	{
		get => AppUtil.IsAppFolderInPath;
		set => AppUtil.AddToPath(value);
	}

	#endregion

	#region

	public SharedInfo Shared { get; set; }

	private readonly ConcurrentDictionary<UniResultItem, string> m_uni;

	private readonly ConcurrentDictionary<string, SearchQuery> m_queries;

	private BitmapImage? m_image;

	private CancellationTokenSource m_cts;
	private CancellationTokenSource m_ctsu;

	private int m_cntResults;

	private readonly ConcurrentDictionary<SearchQuery, ObservableCollection<ResultItem>> m_resultMap;

	private readonly ConcurrentDictionary<SearchQuery, BitmapImage?> m_images;

	#endregion

	#region Queue/Query

	private string m_currentQueueItem;

	public string CurrentQueueItem
	{
		get { return m_currentQueueItem; }
		set
		{

			if (Equals(value, m_currentQueueItem) /*|| String.IsNullOrWhiteSpace(value)*/) return;
			m_currentQueueItem = value;
			OnPropertyChanged();

		}
	}

	public ObservableCollection<string> Queue { get; }

	public bool IsQueueInputValid => !String.IsNullOrWhiteSpace(CurrentQueueItem);

	private void OnCurrentQueueItemChanged(object? sender, PropertyChangedEventArgs args)
	{
		Debug.WriteLine($"{sender} {args}");

		Dispatcher.InvokeAsync(async () =>
		{
			var ok = SearchQuery.IsValidSourceType(CurrentQueueItem);

			if (ok /*&& !IsInputReady()*/) {
				await UpdateQueryAsync(CurrentQueueItem);
			}
			// Btn_Run.IsEnabled = ok;
		});

	}

	private async Task UpdateQueryAsync(string query)
	{
		/*if (await m_us.WaitAsync(TimeSpan.Zero)) {
			Debug.WriteLine($"blocking");
			return;
		}*/

		Btn_Run.IsEnabled = false;
		bool b2;

		bool queryExists = b2 = m_queries.TryGetValue(query, out var existingQuery);
		Tb_Status2.Text = null;

		if (queryExists) {
			// Require.NotNull(existingQuery);
			// assert(existingQuery!= null);
			Debug.Assert(existingQuery != null);
			Query = existingQuery;
		}

		else {
			Query = await SearchQuery.TryCreateAsync(query, m_ctsu.Token);
			// Pb_Status.Foreground      = new SolidColorBrush(Colors.Green);
			Pb_Status.IsIndeterminate = true;
			queryExists               = Query != SearchQuery.Null;
		}

		if (queryExists) {
			Url upload;

			Debug.Assert(Query != null);
			var uriString = Query.ValueString;
			Debug.Assert(uriString != null);

			Tb_Status.Text = "Rendering preview...";

			Dispatcher.InvokeAsync(UpdateImage);

			if (b2) {
				upload         = Query.Upload;
				Tb_Status.Text = $"{Strings.Constants.HEAVY_CHECK_MARK}";
			}
			else {
				Tb_Status.Text = "Uploading...";
				upload         = await Query.UploadAsync(ct: m_ctsu.Token);

				if (!Url.IsValid(upload)) {
					// todo: show user specific error message
					Pb_Status.IsIndeterminate = false;
					Tb_Status.Text            = "-";
					Tb_Status2.Text           = "Failed to upload: server timed out or input was invalid";
					Btn_Run.IsEnabled         = true;
					// Btn_Delete.IsEnabled      = true;
					goto ret;
					// return;
				}
				else {
					Tb_Status.Text = "Uploaded";

				}

			}

			Tb_Upload.Text            = upload;
			Pb_Status.IsIndeterminate = false;

			Btn_Delete.IsEnabled = true && Query.Uni.IsFile;
			// Btn_Remove.IsEnabled = Btn_Delete.IsEnabled;

			if (Query.Uni.IsFile) {
				Img_Type.Source = AppComponents.image;
			}
			else if (Query.Uni.IsUri) {
				Img_Type.Source = AppComponents.image_link;
			}

			/*Tb_Info.Text = $"[{Query.Uni.SourceType}] {Query.Uni.FileTypes[0]} " +
			               $"{FormatHelper.FormatBytes(Query.Uni.Stream.Length)} ";

			if (m_image != null) {
				Tb_Info.Text += $"({m_image.PixelWidth}x{m_image.PixelHeight})";
			}*/

			m_queries.TryAdd(query, Query);

			var resultsFound = m_resultMap.TryGetValue(Query, out var res);

			if (!resultsFound) {
				m_resultMap[Query] = new ObservableCollection<ResultItem>();

			}

			Results                = m_resultMap[Query];
			Lv_Results.ItemsSource = Results;

			if ((Config.AutoSearch && !Client.IsRunning) && !Results.Any()) {
				Dispatcher.InvokeAsync(RunAsync);
			}

			else if (queryExists && Results.Any()) {
				Tb_Status.Text = $"Displaying {Results.Count} results";
			}
			else {
				Tb_Status.Text = $"Search ready";

			}
		}
		else { }

		ret:

		if (!queryExists) {
			Pb_Status.IsIndeterminate = false;
			// Pb_Status.Foreground      = new SolidColorBrush(Colors.Red);
		}

		Btn_Run.IsEnabled = queryExists;
		m_us.Release();
		Debug.WriteLine($"finished {query}");
		return;

		void UpdateInfo()
		{
			Tb_Info.Text =
				ControlsHelper.FormatDescription("Query", Query.Uni, m_image?.PixelWidth, m_image?.PixelHeight);
		}

		void UpdateImage()
		{

			if (m_images.TryGetValue(Query, out m_image)) {
				Debug.WriteLine($"Found cached {Query}");
			}
			else {
				m_image = new BitmapImage()
					{ };
				m_image.BeginInit();
				m_image.UriSource = new Uri(Query.ValueString);
				// m_image.StreamSource   = Query.Uni.Stream;
				m_image.CacheOption    = BitmapCacheOption.OnLoad;
				m_image.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.Default);

				m_image.EndInit();

				m_images[Query] = m_image;
			}

			if (Query.Uni.IsUri) {
				m_image.DownloadCompleted += (sender, args) =>
				{
					UpdateInfo();
				};

			}
			else {
				UpdateInfo();
			}

			if (m_image.CanFreeze) {
				m_image.Freeze();

			}

			// Img_Preview.Source = m_image;

			UpdatePreview();
		}
	}

	private void AddToQueue(IReadOnlyList<string> files)
	{
		if (!files.Any()) {
			return;
		}

		if (!IsQueueInputValid) {
			var ff = files[0];
			CurrentQueueItem = ff;
			Debug.WriteLine($"cqi {ff}");
			// Lv_Queue.SelectedItems.Add(ff);
		}

		int c = 0;

		foreach (var s in files) {

			if (!Queue.Contains(s)) {
				Queue.Add(s);
				Debug.WriteLine($"Added {s}");

				c++;
			}
		}

		Tb_Status2.Text = $"Added {c} items to queue";
	}

	private void AdvanceQueue(int i = 1)
	{
		string next;

		if (Queue.Count == 0) {
			next = String.Empty;
		}
		else next = Queue[(Queue.IndexOf(CurrentQueueItem) + i) % Queue.Count];

		CurrentQueueItem = next;
	}

	#endregion

	#region

	private readonly DispatcherTimer m_trDispatch;

	private async void IdleDispatchAsync(object? sender, EventArgs e) { }

	#endregion

	#region Clipboard

	public bool UseClipboard
	{
		get => Config.Clipboard;
		set => Config.Clipboard = m_cbDispatch.IsEnabled = value;
	}

	private readonly DispatcherTimer m_cbDispatch;

	private readonly List<string> m_clipboard;

	private static int _clipboardSequence;

	[DebuggerHidden]
	private void ClipboardListenAsync(object? s, EventArgs e)
	{
		/*if (IsInputReady() /*|| Query != SearchQuery.Null#1#) {
			return;
		}*/

		var cImg  = Clipboard.ContainsImage();
		var cText = Clipboard.ContainsText();
		var cFile = Clipboard.ContainsFileDropList();

		var seq = Clipboard2.SequenceNumber;

		if (_clipboardSequence == seq) {
			return;
		}
		else {
			_clipboardSequence = seq;
		}

		if (cImg) {

			var bmp = Clipboard.GetImage();
			// var df=DataFormats.GetDataFormat((int) ClipboardFormat.PNG);
			// var fn = Path.GetTempFileName().Split('.')[0] + ".png";
			var fn = FileSystem.GetTempFileName(ext: "png");

			var ms = File.Open(fn, FileMode.OpenOrCreate);
			CurrentQueueItem = fn;
			BitmapEncoder enc = new PngBitmapEncoder();
			enc.Frames.Add(BitmapFrame.Create(bmp));
			enc.Save(ms);
			ms.Dispose();

			AddToQueue(new[] { fn });
			/*Dispatcher.InvokeAsync(() =>
			{
				return SetQueryAsync(fn);

			});*/
		}

		else if (cText) {
			var txt = (string) Clipboard.GetData(DataFormats.UnicodeText);
			txt = txt.CleanString();

			if (SearchQuery.IsValidSourceType(txt)) {

				if ( /*!IsInputReady() && */ !Queue.Contains(txt) && !m_clipboard.Contains(txt)) {
					m_clipboard.Add(txt);
					// Queue.Add(txt);
					// InputText = txt;

					AddToQueue(new[] { txt });

					// await SetQueryAsync(txt);
				}
			}

		}

		else if (cFile) {
			var files = Clipboard.GetFileDropList();
			var rg    = new string[files.Count];
			files.CopyTo(rg, 0);
			rg = rg.Where(x => !m_clipboard.Contains(x) && SearchQuery.IsValidSourceType(x)).ToArray();
			AddToQueue(rg);

			m_clipboard.AddRange(rg);

		}

		if (cFile || cImg || cText) {
			Img_Status.Source = AppComponents.clipboard_invoice;

		}

		// Thread.Sleep(1000);
	}

	#endregion

	#region

	private async Task RunAsync()
	{
		Lb_Queue.IsEnabled = false;
		// ClearResults();
		SearchStart  = DateTime.Now;
		m_cntResults = 0;

		Btn_Run.IsEnabled = false;
		Tb_Status.Text    = "Initiating search...";
		var r = await Client.RunSearchAsync(Query, reload: false, token: m_cts.Token);
	}

	private void OnComplete(object sender, SearchResult[] e)
	{
		Lb_Queue.IsEnabled = true;

		if (m_cts.IsCancellationRequested) {
			Tb_Status.Text = "Cancelled";
		}
		else {
			Tb_Status.Text = "Search complete";
		}

		Btn_Run.IsEnabled = IsQueueInputValid;
		// m_resultMap[Query] = Results;
	}

	private void OnResult(object o, SearchResult result)
	{
		++m_cntResults;
		var cle = Client.Engines.Length;

		Tb_Status.Text  = $"{m_cntResults}/{cle} | {(DateTime.Now - SearchStart).TotalSeconds:F3} sec";
		Pb_Status.Value = (m_cntResults / (double) cle) * 100;

		lock (m_lock) {
			AddResult(result);
		}
	}

	private void AddResult(SearchResult result)
	{
		int i = 0;

		var allResults = result.Results;

		var sri1 = new SearchResultItem(result)
		{
			Url = result.RawUrl,
		};

		Results.Add(new ResultItem(sri1, $"{sri1.Root.Engine.Name} (Raw)"));

		foreach (SearchResultItem sri in allResults) {
			Results.Add(new ResultItem(sri, $"{sri.Root.Engine.Name} #{++i}"));
			int j = 0;

			foreach (var ssri in sri.Sisters) {
				Results.Add(new ResultItem(ssri, $"{ssri.Root.Engine.Name} #{i}.{++j}"));

			}
		}
	}

	#endregion

	#region

	private void ReloadToken()
	{
		m_cts  = new();
		m_ctsu = new();
	}

	private void Cancel()
	{
		m_cts.Cancel();
		m_ctsu.Cancel();
		// Pb_Status.Foreground = new SolidColorBrush(Colors.Red);
		Pb_Status.IsIndeterminate = false;
	}

	private void Restart(bool full = false)
	{
		Cancel();
		ClearResults(full);
		Dispose(full);

		CurrentQueueItem = String.Empty;
		ReloadToken();
	}

	private void ClearQueryControls()
	{
		m_image = null;
		m_images.TryRemove(Query, out var img);
		Img_Preview.Source = null;
		Img_Preview.UpdateLayout();
		Tb_Status.Text   = String.Empty;
		CurrentQueueItem = String.Empty;
		Tb_Info.Text     = String.Empty;
		Tb_Status2.Text  = String.Empty;

		Tb_Upload.Text            = String.Empty;
		Pb_Status.IsIndeterminate = false;
	}

	private void ClearResults(bool full = false)
	{
		m_cntResults = 0;

		if (full) {
			/*foreach (var r in Results) {
				r.Dispose();
			}*/

			Results.Clear();
			// ClearQueryControls();
		}

		// Btn_Run.IsEnabled = true;
		// Query.Dispose();
		Pb_Status.Value = 0;
		Tb_Search.Text  = String.Empty;

		// Tb_Status.Text  = string.Empty;
	}

	public void Dispose()
	{
		Dispose(true);
	}

	private void Reset()
	{
		Restart(true);
		ClearQueryControls();
		CurrentQueueItem = String.Empty;
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
	}

	public void Dispose(bool full)
	{

		if (full) {
			// Client.Dispose();
			Query.Dispose();
			Query = SearchQuery.Null;

			Queue.Clear();
			// QueueSelectedIndex = 0;

			foreach (var kv in m_queries) {
				kv.Value.Dispose();
			}

			m_queries.Clear();

			foreach (var kv in m_uni) {
				kv.Key.Dispose();
			}

			m_uni.Clear();
			m_clipboard.Clear();

			foreach (var r in Results) {
				r.Dispose();
			}

			Results.Clear();

			foreach ((SearchQuery key, ObservableCollection<ResultItem> value) in m_resultMap) {
				key.Dispose();
				value.Clear();
			}

			m_resultMap.Clear();

			m_image            = null;
			Img_Preview.Source = m_image;
			Img_Preview.UpdateLayout();

			m_images.Clear();
			m_us.Release();
		}

		m_cts.Dispose();
		m_ctsu.Dispose();
	}

	#endregion

	#region

	public DateTime SearchStart { get; private set; }

	#endregion

	#region

	private async Task DownloadResultAsync(UniResultItem uri)
	{
		await uri.DownloadAsync();
		m_uni.TryAdd(uri, uri.Download);
		Tb_Status.Text = $"Downloaded to {uri.Download}";
	}

	private async Task ScanResultAsync(ResultItem ri)
	{
		if (!ri.CanScan) {
			return;
		}

		Tb_Status.Text            = "Scanning...";
		Pb_Status.IsIndeterminate = true;
		ri.StatusImage            = AppComponents.arrow_refresh;

		bool d = false;

		try {
			d = await ri.Result.LoadUniAsync(m_cts.Token);

			if (d) {
				Debug.WriteLine($"{ri}");
				var resultUni = ri.Result.Uni;

				// var resultItems = new ResultItem[resultUni.Length];
				for (int i = 0; i < resultUni.Length; i++) {
					var rii = new UniResultItem(ri, i)
					{
						StatusImage = AppComponents.picture_link,
						CanDownload = true,
						CanScan     = false,
						CanOpen     = true
					};
					// resultItems[i] = rii;
					Results.Insert(Results.IndexOf(ri) + 1 + i, rii);
				}

				int length = ri.Result.Uni.Length;

				if (length > 0) {
					ri.StatusImage = AppComponents.pictures;
				}

				Tb_Status.Text = $"Scan found {length} images";
			}
			else {
				Tb_Status.Text = $"Scan found no images";

			}

		}
		catch (Exception e) {
			d = false;
			Log(new($"scan: {e.Message}"));

			if (e is TaskCanceledException) {
				Tb_Status.Text = $"Cancelled";
			}

		}
		finally {
			ri.CanScan = !d;

			if (!d) {
				ri.StatusImage = AppComponents.picture_empty;
			}

			Pb_Status.IsIndeterminate = false;

		}
	}

	private async Task ScanGalleryResultAsync(ResultItem cri)
	{

		if (FileSystem.FindInPath("gallery-dl.exe") == null) {
			MessageBox.Show(this, "gallery-dl not in path");
			return;
		}

		Tb_Status.Text            = "Scanning with gallery-dl";
		Pb_Status.IsIndeterminate = true;

		try {
			var rg = await BaseImageHost.RunGalleryAsync(cri.Url, m_cts.Token);
			cri.Result.Uni = rg;

			for (int i = 0; i < rg.Length; i++) {
				var rii = new UniResultItem(cri, i)
				{
					StatusImage = AppComponents.picture,
					CanDownload = true,
					CanScan     = false,
					CanOpen     = true
				};
				Results.Insert(Results.IndexOf(cri) + 1 + i, rii);
			}

			Tb_Status.Text = "Scanned with gallery-dl";
		}
		catch (Exception e) {
			if (e is TaskCanceledException) {
				Tb_Status.Text = $"Cancelled";
			}

		}
		finally {
			Pb_Status.IsIndeterminate = false;
		}
	}

	private async Task FilterResultsAsync()
	{
		Btn_Filter.IsEnabled = false;

		Pb_Status.IsIndeterminate = true;
		Tb_Status.Text            = "Filtering";

		var cb = new ConcurrentBag<ResultItem>(Results);
		int c  = 0;

		await Parallel.ForEachAsync(cb, async (item, token) =>
		{
			if (!Url.IsValid(item.Url)) {
				return;
			}

			var res = await item.GetResponseAsync(token);

			if (res is { ResponseMessage.IsSuccessStatusCode: false }) {
				Debug.WriteLine($"removing {item}");

				Dispatcher.Invoke(() =>
				{
					++c;
					Results.Remove(item);
				});
			}
			else { }

		});

		Debug.WriteLine("continuing");
		Tb_Status.Text            = $"Filtered {c}";
		Pb_Status.IsIndeterminate = false;
		Btn_Filter.IsEnabled      = true;
	}

	private async Task EnqueueResultAsync(UniResultItem uri)
	{
		// var d = await uri.Uni.TryDownloadAsync();
		var d = await uri.DownloadAsync(Path.GetTempPath(), false);
		AddToQueue(new[] { d });
		// CurrentQueueItem = d;
	}

	private async void RetryEngineAsync(ResultItem ri)
	{
		var eng = ri.Result.Root.Engine;
		Tb_Status.Text = $"Retrying {eng.Name}";
		var idx = FindIndex(r => r.Result.Root.Engine == eng);
		var fi  = idx;

		for (int i = Results.Count - 1; i >= 0; i--) {
			if (Results[i].Result.Root.Engine == eng) {
				Results.RemoveAt(i);
			}
		}

		Pb_Status.IsIndeterminate = true;
		var result = await eng.GetResultAsync(Query, m_cts.Token);
		AddResult(result);
		Tb_Status.Text            = $"{eng.Name} → {result.Results.Count}";
		Pb_Status.IsIndeterminate = false;

		for (int i = 0; i < Results.Count; i++) {
			var cr = Results[i];

			if (cr.Result.Root.Engine == eng) {
				Results.Move(i, idx++);

			}
		}

		Lv_Results.ScrollIntoView(Results[fi]);
	}

	#endregion

	#region

	public static readonly string[] Args = Environment.GetCommandLineArgs();

	public static readonly Dictionary<string, Func<ResultItem, string?>> SearchFields = new()
	{

		{
			nameof(ResultItem.Name), (x) => x.Name
		},
		{
			nameof(ResultItem.Url), (x) => x.Url?.ToString()
		},
		{
			nameof(ResultItem.Result.Description), (x) => x.Result.Description
		},
	};

	private void ParseArgs(string[] args)
	{
		// Logs.Add(new(args.QuickJoin()));

		var e = args.GetEnumerator();

		string inp = null;

		while (e.MoveNext()) {
			if (e.Current is not string c) {
				break;
			}

			if (c == R2.Arg_Input) {
				inp = (string) e.MoveAndGet();

				// CurrentQueueItem = inp;
				AddToQueue(new[] { inp });

				continue;
			}

			if (c == R2.Arg_Switch && inp != null) {
				CurrentQueueItem = inp;

			}

			if (c == R2.Arg_AutoSearch) {

				Config.AutoSearch = true;
			}

			if (c == R2.Arg_Hide) {

				if (IsVisible) {
					Visibility = Visibility.Hidden;
				}
				else {
					Visibility = Visibility.Visible;
				}
			}
		}

	}

	#endregion

	private void ChangeStatus2(ResultItem ri)
	{
		if (ri is UniResultItem { Uni: { } } uri) {
			Tb_Status2.Text = uri.Description;
		}
		else {
			Tb_Status2.Text = $"{ri.Name}";
			
		}
	}

	#region

	private void Log(LogEntry l)
	{
		Logs.Add(l);
		Debug.WriteLine(l);
	}

	public ObservableCollection<LogEntry> Logs { get; }

	#endregion

	private async void CheckForUpdate()
	{
		var cv = AppUtil.Version;
		var lv = await AppUtil.GetLatestReleaseAsync();

		Tb_Version.Text = $"{cv}";
		// Tb_Version2.Text = $"{lv.Version}";

		if (lv is { Version: { } }) {
			Tb_Version2.Text = $"{lv.Version}";

			if (lv.Version > cv) {
				Tb_Version2.Text += $"(click to download)";
				var url = lv.assets[0].browser_download_url;

				Tb_Version2.MouseDown += async (o, args) =>
				{
					FileSystem.Open(url);
					// await url.GetStreamAsync();
					args.Handled = true;
				};
			}
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	/*public delegate bool PreviewChangedCallback(ResultItem? ri);

	public event PreviewChangedCallback? PreviewChanged;*/

	#region

	private void UpdatePreview(ResultItem ri)
	{
		/*if (ri.Load()) {
			UpdatePreview(ri.Image);
		}*/

		Application.Current.Dispatcher.InvokeAsync(() =>
		{
			if (ri.LoadImage()) {
				Img_Preview.Source = ri.Image;
				// Debug.WriteLine($"updated image {ri.Image}");
				// PreviewChanged?.Invoke(ri);
				Tb_Preview.Text = $"Preview: {ri.Name}";
				
			}
			else {
				UpdatePreview();
			}
		});
	}

	private void UpdatePreview()
	{
		UpdatePreview(m_image);
		
	}

	private void UpdatePreview(ImageSource x)
	{
		Application.Current.Dispatcher.Invoke(() =>
		{
			Img_Preview.Source = x;
			// Debug.WriteLine($"updated image {x}");
			// PreviewChanged?.Invoke(null);
		});
	}

	#endregion

	private void OnPipeReceived(string s)
	{
		Dispatcher.Invoke(() =>
		{
			if (s[0] == App.ARGS_DELIM) {
				var parentId = int.Parse(s[1..]);
				Tb_Status.Text = $"Received data (IPC) {parentId}";

				ParseArgs(m_pipeBuffer.ToArray());
				m_pipeBuffer.Clear();
				AppUtil.FlashTaskbar(m_wndInterop.Handle);
			}
			else {
				m_pipeBuffer.Add(s);
			}

		});

	}
}

public class ResultModel
{
	//todo
	public string                           Value   { get; set; }
	public SearchQuery                      Query   { get; set; }
	public BitmapImage                      Image   { get; set; }
	public ObservableCollection<ResultItem> Results { get; set; }

	public async Task Init(string query) { }
}