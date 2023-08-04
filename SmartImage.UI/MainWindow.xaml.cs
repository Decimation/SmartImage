global using Clipboard2 = Novus.Win32.Clipboard;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Cache;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Flurl;
using Kantan.Collections;
using Kantan.Net.Utilities;
using Kantan.Numeric;
using Kantan.Text;
using Kantan.Utilities;
using Microsoft.Extensions.Logging;
using Novus.FileTypes;
using Novus.OS;
using Novus.Streams;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Impl.Upload;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;
using Color = System.Drawing.Color;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace SmartImage.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, IDisposable, INotifyPropertyChanged
{
	private static readonly string[] Args;

	static MainWindow()
	{
		Args = Environment.GetCommandLineArgs();

	}

	public MainWindow()
	{
		Client    = new SearchClient(new SearchConfig());
		m_queries = new ConcurrentDictionary<string, SearchQuery>();

		InitializeComponent();

		DataContext = this;
		SearchStart = default;
		Results     = new();

		Query              = SearchQuery.Null;
		Queue              = new();
		QueueSelectedIndex = 0;
		_clipboardSequence = 0;
		m_cts              = new CancellationTokenSource();
		m_ctsu             = new CancellationTokenSource();
		TimerText          = null;

		Lb_Engines.ItemsSource  = Engines;
		Lb_Engines2.ItemsSource = Engines;
		Lb_Engines.HandleEnum(Config.SearchEngines);
		Lb_Engines2.HandleEnum(Config.PriorityEngines);

		Lv_Results.ItemsSource = Results;
		Lv_Queue.ItemsSource   = Queue;

		Client.OnResult   += OnResult;
		Client.OnComplete += OnComplete;

		m_cbDispatch = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(1)
		};
		m_cbDispatch.Tick += ClipboardListenAsync;

		m_bgDispatch = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(1)
		};
		m_bgDispatch.Tick += BackgroundDispatch;

		m_trDispatch = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(0.25)
		};
		m_trDispatch.Tick += TimerDispatch;

		m_uni                    = new();
		m_clipboard              = new();
		Cb_ContextMenu.IsChecked = AppUtil.IsContextMenuAdded;
		m_resultMap              = new();

		ParseArgs();

		m_image = null;

		Rb_UploadEngine_Catbox.IsChecked    = BaseUploadEngine.Default is CatboxEngine;
		Rb_UploadEngine_Litterbox.IsChecked = BaseUploadEngine.Default is LitterboxEngine;

		BindingOperations.EnableCollectionSynchronization(Results, m_lock);
		RenderOptions.SetBitmapScalingMode(Img_Preview, BitmapScalingMode.HighQuality);

	}

	#region

	private static readonly ILogger Logger = LogUtil.Factory.CreateLogger(nameof(MainWindow));

	public static SearchEngineOptions[] Engines { get; } = Enum.GetValues<SearchEngineOptions>();

	private readonly object m_lock = new();

	#endregion

	#region

	public SearchClient Client { get; }

	public SearchConfig Config => Client.Config;

	public SearchQuery Query { get; internal set; }

	public ObservableCollection<ResultItem> Results { get; private set; }

	public bool UseClipboard
	{
		get => Config.Clipboard;
		set => Config.Clipboard = m_cbDispatch.IsEnabled = value;
	}

	public bool UseContextMenu
	{
		get => AppUtil.IsContextMenuAdded;
		set => AppUtil.HandleContextMenu(value);
	}

	#endregion

	#region

	private readonly ConcurrentDictionary<UniResultItem, string> m_uni;

	private readonly DispatcherTimer m_cbDispatch;
	private readonly DispatcherTimer m_bgDispatch;
	private readonly DispatcherTimer m_trDispatch;

	private readonly ConcurrentDictionary<string, SearchQuery> m_queries;

	private BitmapImage? m_image;

	private CancellationTokenSource m_cts;
	private CancellationTokenSource m_ctsu;

	private int m_cntResults;

	private readonly ConcurrentDictionary<SearchQuery, ObservableCollection<ResultItem>> m_resultMap;

	private readonly List<string> m_clipboard;

	private static int _status = S_OK;

	private const int S_NO = 0;
	private const int S_OK = 1;

	private static int _clipboardSequence;

	#endregion

	#region Queue

	private int m_queueSelectedIndex;

	public int QueueSelectedIndex
	{
		get => m_queueSelectedIndex;
		set
		{
			if (value == m_queueSelectedIndex) return;
			m_queueSelectedIndex = value;
			OnPropertyChanged();
		}
	}

	private string m_queueSelectedItem;

	public string QueueSelectedItem
	{
		get => m_queueSelectedItem;
		set
		{
			if (Equals(value, m_queueSelectedItem)) return;
			m_queueSelectedItem = value;
			OnPropertyChanged();
		}
	}

	public ObservableCollection<string> Queue { get; }

	public bool TrySeekQueue(int i)
	{
		var b = MathHelper.IsInRange(i, Queue.Count) && Queue.Count > 0;

		if (b) {
			QueueSelectedIndex = i;
		}

		return b;
	}

	public bool IsQueueInputValid => !string.IsNullOrWhiteSpace(QueueSelectedItem);

	private async Task SetQueryAsync()
	{
		var query = QueueSelectedItem;
		Interlocked.Exchange(ref _status, S_NO);

		Btn_Run.IsEnabled = false;
		bool b2;

		bool queryExists = b2 = m_queries.TryGetValue(query, out var existingQuery);

		if (queryExists) {
			Query = existingQuery;
		}

		else {
			Query                     = await SearchQuery.TryCreateAsync(query, m_ctsu.Token);
			Pb_Status.IsIndeterminate = true;
			queryExists               = Query != SearchQuery.Null;
		}

		if (queryExists) {
			Url upload;

			if (b2) {
				upload         = Query.Upload;
				Tb_Status.Text = $"{Strings.Constants.HEAVY_CHECK_MARK}";

			}
			else {
				Tb_Status.Text = "Uploading...";
				upload         = await Query.UploadAsync(ct: m_ctsu.Token);
				Tb_Status.Text = "Uploaded";

				if (!Url.IsValid(upload)) {
					Pb_Status.IsIndeterminate = false;
					Tb_Status.Text            = "-";
					Tb_Info2.Text             = "Invalid";
					Btn_Run.IsEnabled         = true;
					// Btn_Delete.IsEnabled      = true;

					return;
				}

			}

			Tb_Upload.Text            = upload;
			Pb_Status.IsIndeterminate = false;

			m_image = new BitmapImage()
				{ };
			m_image.BeginInit();
			m_image.UriSource      = new Uri(Query.Uni.Value.ToString());
			m_image.CacheOption    = BitmapCacheOption.OnLoad;
			m_image.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.Default);
			m_image.EndInit();
			Img_Preview.Source = m_image;

			Btn_Delete.IsEnabled = true && Query.Uni.IsFile;

			if (Query.Uni.IsFile) {
				Img_Type.Source = AppComponents.image;
			}
			else if (Query.Uni.IsUri) {
				Img_Type.Source = AppComponents.image_link;
			}

			Tb_Info.Text = $"[{Query.Uni.SourceType}] {Query.Uni.FileTypes[0]}" +
			               $" {FormatHelper.FormatBytes(Query.Uni.Stream.Length)}/{FormatHelper.FormatBytes(Query.Size)}";

			m_queries.TryAdd(query, Query);

			var resultsFound = m_resultMap.TryGetValue(Query, out var res);

			if (!resultsFound) {
				m_resultMap[Query] = new ObservableCollection<ResultItem>();

			}

			Results                = m_resultMap[Query];
			Lv_Results.ItemsSource = Results;

			if ((Config.AutoSearch && !Client.IsRunning) && !Results.Any()) {
				Application.Current.Dispatcher.InvokeAsync(RunAsync);
			}
		}

		Btn_Run.IsEnabled = queryExists;
		Interlocked.Exchange(ref _status, S_OK);

	}

	private void AddToQueueAsync(string[] files)
	{
		if (!files.Any()) {
			return;
		}

		if (!IsQueueInputValid) {
			var ff = files[0];
			QueueSelectedItem = ff;

			// Lv_Queue.SelectedItems.Add(ff);
		}

		int c = 0;

		foreach (var s in files) {

			if (!Queue.Contains(s)) {
				Queue.Add(s);

				c++;
			}
		}

		Tb_Info2.Text = $"Added {c} items to queue";
	}

	#endregion

	#region

	public DateTime SearchStart { get; private set; }

	private string m_timerText;

	public string TimerText
	{
		get => m_timerText;
		set
		{
			if (value == m_timerText) return;
			m_timerText = value;
			OnPropertyChanged();
		}
	}

	private void TimerDispatch(object? sender, EventArgs e)
	{
		TimerText = $"{(DateTime.Now - SearchStart).TotalSeconds:F3} sec";
	}

	#endregion

	private void BackgroundDispatch(object? sender, EventArgs e) { }

	private async Task RunAsync()
	{
		Lv_Queue.IsEnabled = false;
		// ClearResults();
		SearchStart = DateTime.Now;
		m_trDispatch.Start();

		var r = await Client.RunSearchAsync(Query, token: m_cts.Token);
	}

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
			QueueSelectedItem = fn;
			BitmapEncoder enc = new PngBitmapEncoder();
			enc.Frames.Add(BitmapFrame.Create(bmp));
			enc.Save(ms);
			ms.Dispose();

			AddToQueueAsync(new[] { fn });
			/*Application.Current.Dispatcher.InvokeAsync(() =>
			{
				return SetQueryAsync(fn);

			});*/
		}

		else if (cText) {
			var txt = (string) Clipboard.GetData(DataFormats.Text);

			if (SearchQuery.IsValidSourceType(txt)) {

				if ( /*!IsInputReady() && */ !Queue.Contains(txt) && !m_clipboard.Contains(txt)) {
					m_clipboard.Add(txt);
					// Queue.Add(txt);
					// InputText = txt;

					AddToQueueAsync(new[] { txt });

					// await SetQueryAsync(txt);
				}
			}

		}

		else if (cFile) {
			var files = Clipboard.GetFileDropList();
			var rg    = new string[files.Count];
			files.CopyTo(rg, 0);
			rg = rg.Where(x => !m_clipboard.Contains(x) && SearchQuery.IsValidSourceType(x)).ToArray();
			AddToQueueAsync(rg);

			m_clipboard.AddRange(rg);

		}

		if (cFile || cImg || cText) {
			Img_Status.Source = AppComponents.clipboard_invoice;

		}

		// Thread.Sleep(1000);
	}

	private void OnComplete(object sender, SearchResult[] e)
	{
		m_trDispatch.Stop();
		Lv_Queue.IsEnabled = true;

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

		Tb_Status.Text  = $"{m_cntResults}/{cle}";
		Pb_Status.Value = (m_cntResults / (double) cle) * 100;

		lock (m_lock) {
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
	}

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
	}

	private void Restart(bool full = false)
	{
		Cancel();
		ClearResults(full);
		Dispose(full);

		QueueSelectedItem = string.Empty;

		ReloadToken();
	}

	private void ClearQueryControls()
	{
		m_image            = null;
		Img_Preview.Source = null;
		Img_Preview.UpdateLayout();
		Tb_Status.Text            = string.Empty;
		QueueSelectedItem                = string.Empty;
		Tb_Info.Text              = string.Empty;
		Tb_Info2.Text             = string.Empty;
		TimerText                 = String.Empty;
		Tb_Upload.Text            = string.Empty;
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
	}

	public void Dispose(bool full)
	{

		if (full) {
			// Client.Dispose();
			Query.Dispose();
			Query = SearchQuery.Null;

			Queue.Clear();
			QueueSelectedIndex = 0;

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
		}

		m_cts.Dispose();
		m_ctsu.Dispose();
	}

	#endregion

	private void ChangeInfo2(ResultItem ri)
	{
		if (ri is UniResultItem { Uni: { } } uri) {
			Tb_Info2.Text = $"{uri.Uni.FileTypes[0]}";

		}
		else {
			Tb_Info2.Text = $"{ri.Name}";
		}
	}

	private async Task DownloadResultAsync(UniResultItem ri)
	{
		var uni = ri.Uni;

		var    url  = (Url) uni.Value.ToString();
		string path = url.GetFileName();

		var path2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), path);

		var fs = File.OpenWrite(path2);
		uni.Stream.TrySeek();

		ri.StatusImage = AppComponents.picture_save;
		await uni.Stream.CopyToAsync(fs);
		FileSystem.ExploreFile(path2);
		fs.Dispose();
		ri.CanDownload = false;
		m_uni.TryAdd(ri, path2);

		// u.Dispose();
	}

	private async Task ScanResultAsync(ResultItem ri)
	{
		if (!ri.CanScan) {
			return;
		}

		Pb_Status.IsIndeterminate = true;

		bool d;

		try {
			d = await ri.Result.LoadUniAsync(m_cts.Token);
		}
		catch (Exception e) {
			d = false;
			Debug.WriteLine($"{e.Message}");
		}

		ri.CanScan = !d;

		if (d) {
			Debug.WriteLine($"{ri}");
			var resultUni   = ri.Result.Uni;
			var resultItems = new ResultItem[resultUni.Length];

			for (int i = 0; i < resultUni.Length; i++) {
				var rii = new UniResultItem(ri, i)
				{
					StatusImage = AppComponents.picture,
					CanDownload = true,
					CanScan     = false,
					CanOpen     = true
				};
				resultItems[i] = rii;
				Results.Insert(Results.IndexOf(ri) + 1 + i, rii);
			}
		}

		Pb_Status.IsIndeterminate = false;

	}

	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private void ParseArgs()
	{
		var e = Args.GetEnumerator();

		foreach (var arg in Args) {
			Tb_Log.Text += $"{arg}\n";
		}

		while (e.MoveNext()) {
			var c = e.Current.ToString();

			if (c == R2.Arg_Input) {
				var inp = e.MoveAndGet();
				QueueSelectedItem = inp.ToString();
				continue;
			}

			if (c == R2.Arg_AutoSearch) {

				e.MoveNext();
				Config.AutoSearch = true;
			}
		}
	}
}