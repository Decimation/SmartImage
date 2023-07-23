using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Cache;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Flurl;
using Kantan.Collections;
using Kantan.Net.Utilities;
using Microsoft.Extensions.Logging;
using Novus.FileTypes;
using Novus.OS;
using SmartImage.Lib;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Impl.Upload;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartImage.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, IDisposable
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

		foreach (var arg in Args) {
			Tb_Log.Text += $"{arg}\n";
		}

		DataContext = this;
		Results     = new();

		Query      = SearchQuery.Null;
		Queue      = new();
		m_queuePos = 0;
		m_cts      = new CancellationTokenSource();
		m_ctsu     = new CancellationTokenSource();

		Engines1                = new(Engines);
		Engines2                = new(Engines);
		Lb_Engines.ItemsSource  = Engines1;
		Lb_Engines2.ItemsSource = Engines2;
		Lb_Engines.HandleEnumList(Engines1, Config.SearchEngines);
		Lb_Engines2.HandleEnumList(Engines2, Config.PriorityEngines);

		Lv_Results.ItemsSource = Results;
		Lv_Queue.ItemsSource   = Queue;

		Client.OnResult   += OnResult;
		Client.OnComplete += OnComplete;

		m_cbDispatch = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(1)
		};
		m_cbDispatch.Tick += ClipboardListenAsync;

		m_uni                    = new();
		m_clipboard              = new();
		Cb_ContextMenu.IsChecked = AppUtil.IsContextMenuAdded;
		m_resultMap              = new();

		var e = Args.GetEnumerator();

		while (e.MoveNext()) {
			var c = e.Current.ToString();

			if (c == R2.Arg_Input) {
				var inp = e.MoveAndGet();
				InputText = inp.ToString();
				continue;
			}

			if (c == R2.Arg_AutoSearch) {

				e.MoveNext();
				Config.AutoSearch = true;
			}
		}

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

	/// <summary>
	/// <see cref="Lb_Engines"/>
	/// <see cref="SearchConfig.SearchEngines"/>
	/// </summary>
	public List<SearchEngineOptions> Engines1 { get; }

	/// <summary>
	/// <see cref="Lb_Engines2"/>
	/// <see cref="SearchConfig.PriorityEngines"/>
	/// </summary>
	public List<SearchEngineOptions> Engines2 { get; }

	public SearchClient Client { get; }

	public SearchConfig Config => Client.Config;

	public SearchQuery Query { get; internal set; }

	public ObservableCollection<ListResultItem> Results { get; set; }

	public ObservableCollection<string> Queue { get; }

	public string InputText
	{
		get => Tb_Input.Text;
		set => Tb_Input.Text = value;
	}

	#endregion

	#region

	private readonly ConcurrentDictionary<ListResultItem, UniSource[]> m_uni;

	private readonly List<string> m_clipboard;

	private readonly DispatcherTimer m_cbDispatch;

	private readonly ConcurrentDictionary<string, SearchQuery> m_queries;

	private int m_queuePos;

	private BitmapImage? m_image;

	private CancellationTokenSource m_cts;
	private CancellationTokenSource m_ctsu;

	private int m_cntResults;

	private static int _status = S_OK;

	private const int S_NO = 0;
	private const int S_OK = 1;

	private readonly ConcurrentDictionary<SearchQuery, ObservableCollection<ListResultItem>> m_resultMap;

	#endregion

	#region

	private async Task RunAsync()
	{
		Clear();
		var r = await Client.RunSearchAsync(Query, token: m_cts.Token);
	}

	private bool IsInputReady()
	{
		return !string.IsNullOrWhiteSpace(InputText);
	}

	private async void ClipboardListenAsync(object? s, EventArgs e)
	{
		if (IsInputReady() /*|| Query != SearchQuery.Null*/) {
			return;
		}

		var cImg  = Clipboard.ContainsImage();
		var cText = Clipboard.ContainsText();
		var cFile = Clipboard.ContainsFileDropList();

		if (cImg) {

			var bmp = Clipboard.GetImage();
			// var df=DataFormats.GetDataFormat((int) ClipboardFormat.PNG);
			var fn = Path.GetTempFileName().Split('.')[0] + ".png";
			var ms = File.Open(fn, FileMode.OpenOrCreate);
			InputText = fn;
			BitmapEncoder enc = new PngBitmapEncoder();
			enc.Frames.Add(BitmapFrame.Create(bmp));
			enc.Save(ms);
			ms.Dispose();

			Application.Current.Dispatcher.InvokeAsync(() =>
			{
				return SetQueryAsync(fn);

			});
		}

		else if (cText) {
			var txt = (string) Clipboard.GetData(DataFormats.Text);

			if (SearchQuery.IsValidSourceType(txt)) {

				if (!IsInputReady() && !m_clipboard.Contains(txt)) {
					m_clipboard.Add(txt);
					InputText = txt;
					// await SetQueryAsync(txt);
				}
			}

		}

		else if (cFile) {
			var files = Clipboard.GetFileDropList();
			var rg    = new string[files.Count];
			files.CopyTo(rg, 0);
			rg = rg.Where(x => !m_clipboard.Contains(x)).ToArray();
			EnqueueAsync(rg);
			m_clipboard.AddRange(rg);

		}

		// Thread.Sleep(1000);
	}

	private async Task SetQueryAsync(string q)
	{
		Interlocked.Exchange(ref _status, S_NO);

		Btn_Run.IsEnabled = false;
		bool b;

		b = m_queries.TryGetValue(q, out var qq);

		if (b) {
			Query = qq;
		}

		else {
			Query                     = await SearchQuery.TryCreateAsync(q, m_ctsu.Token);
			Pb_Status.IsIndeterminate = true;
			b                         = Query != SearchQuery.Null;
		}

		if (b) {
			Tb_Status.Text = "Uploading...";
			var u = await Query.UploadAsync(ct: m_ctsu.Token);
			Tb_Status.Text = "Uploaded";

			if (!Url.IsValid(u)) {
				return;
			}

			Tb_Upload.Text            = u;
			Pb_Status.IsIndeterminate = false;

			Img_Preview.Source = m_image = new BitmapImage(new Uri(Query.Uni.Value.ToString()))
			{
				CacheOption    = BitmapCacheOption.OnLoad,
				UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable)
			};
			Tb_Info.Text = $"[{Query.Uni.SourceType}] {Query.Uni.FileTypes[0]}";

			m_queries.TryAdd(q, Query);

			var ck = m_resultMap.TryGetValue(Query, out var res);

			if (!ck) {
				m_resultMap[Query] = new ObservableCollection<ListResultItem>();

			}

			Results                = m_resultMap[Query];
			Lv_Results.ItemsSource = Results;

			if ((Config.AutoSearch && !Client.IsRunning) && !Results.Any()) {
				Application.Current.Dispatcher.InvokeAsync(RunAsync);
			}
		}

		Btn_Run.IsEnabled = b;
		Interlocked.Exchange(ref _status, S_OK);

	}

	private void OnComplete(object sender, SearchResult[] e)
	{
		if (m_cts.IsCancellationRequested) {
			Tb_Status.Text = "Cancelled";
		}
		else {
			Tb_Status.Text = "Search complete";
		}

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

			var allResults = result.AllResults;

			var sri1 = new SearchResultItem(result)
			{
				Url = result.RawUrl,
			};

			Results.Add(new ListResultItem(sri1, $"{sri1.Root.Engine.Name} (Raw)", result.Status)
			{
				StatusImage = AppComponents.help
			});

			foreach (SearchResultItem sri in allResults) {
				Results.Add(new ListResultItem(sri, $"{sri.Root.Engine.Name} #{++i}", result.Status));

			}
		}
	}

	private void EnqueueAsync(string[] files)
	{
		if (!files.Any()) {
			return;
		}

		if (!IsInputReady()) {
			var ff = files[0];
			InputText = ff;

			// Lv_Queue.SelectedItems.Add(ff);
		}

		int c = 0;

		foreach (var s in files) {

			if (!Queue.Contains(s)) {
				Queue.Add(s);
				c++;
			}
		}

		Tb_Status.Text = $"Added {c} items to queue";
	}

	private void Cancel()
	{

		m_cts.Cancel();
		m_ctsu.Cancel();
	}

	private void Restart(bool full = false)
	{
		Cancel();
		Clear();
		Dispose(full);

		if (full) {
			Results.Clear();

		}

		InputText = string.Empty;

		m_cts  = new();
		m_ctsu = new();
	}

	private void Clear()
	{
		m_cntResults = 0;
		// Results.Clear();
		// Btn_Run.IsEnabled = false;
		// Query.Dispose();
		Pb_Status.Value = 0;
		Tb_Status.Text  = string.Empty;
	}

	private void Next()
	{
		Restart();

		if (!Queue.Any()) {
			return;
		}

		var next = Queue[m_queuePos++ % Queue.Count];
		InputText = next;
		Lv_Queue.SelectedItems.Clear();
		Lv_Queue.SelectedItems.Add(next);

		if (m_queuePos < Queue.Count && m_queuePos >= 0) {
			// await SetQueryAsync(next);
		}
	}

	public void Dispose()
	{
		Dispose(true);
	}

	public void Dispose(bool full)
	{
		if (full) {
			// Client.Dispose();
			Query.Dispose();
			Query = SearchQuery.Null;

			Queue.Clear();
			m_queuePos = 0;

			foreach (var kv in m_queries) {
				kv.Value.Dispose();
			}

			m_queries.Clear();

			foreach (var kv in m_uni) {
				kv.Key.Dispose();
			}

			m_uni.Clear();
			m_clipboard.Clear();

			foreach (var r1 in Results) {
				r1.Dispose();
			}

			Results.Clear();

			foreach ((SearchQuery key, ObservableCollection<ListResultItem> value) in m_resultMap) {
				key.Dispose();
				value.Clear();
			}

			m_resultMap.Clear();
		}

		m_cts.Dispose();
		m_ctsu.Dispose();
	}

	#endregion

	private async Task DownloadResultAsync()
	{
		var ri = ((ListResultItem) Lv_Results.SelectedItem);
		var u  = ri.Uni;

		var    v = (Url) u.Value.ToString();
		string path;

		if (v.PathSegments is { Count: >= 1 }) {
			path = $"{v.PathSegments[^1]}";

		}
		else
			path = v.Path;

		// path = HttpUtility.HtmlDecode(path);
		// path = WebUtility.UrlDecode(path);
		path = HttpUtility.UrlDecode(path);

		path = FileSystem.SanitizeFilename(path);
		var path2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), path);

		var f = File.OpenWrite(path2);

		if (u.Stream.CanSeek) {
			u.Stream.Position = 0;

		}

		ri.StatusImage = AppComponents.picture_save;
		await u.Stream.CopyToAsync(f);
		FileSystem.ExploreFile(path2);
		f.Dispose();
		// u.Dispose();
	}

	private async Task ScanResultAsync()
	{
		var ri = ((ListResultItem) Lv_Results.SelectedItem);

		if (m_uni.ContainsKey(ri)) {
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
		if (d) {
			Debug.WriteLine($"{ri}");
			var resultUni = ri.Result.Uni;
			m_uni.TryAdd(ri, resultUni);
			var resultItems = new ListResultItem[resultUni.Length];

			for (int i = 0; i < resultUni.Length; i++) {
				var rii = new ListResultItem(ri.Result, $"{ri.Name} ({i})", ri.Status, idx: i)
				{
					StatusImage = AppComponents.picture
				};
				resultItems[i] = rii;
				Results.Insert(Results.IndexOf(ri) + 1 + i, rii);
			}
		}

		Pb_Status.IsIndeterminate = false;

	}
}