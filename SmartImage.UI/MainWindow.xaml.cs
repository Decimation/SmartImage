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
using System.Drawing;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime;
using System.Runtime.Caching;
using ReactiveUI;
using Windows.Media.Protection.PlayReady;
using Brush = System.Drawing.Brush;
using Brushes = System.Windows.Media.Brushes;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace SmartImage.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, IDisposable, INotifyPropertyChanged
{

	public const int INVALID = -1;

	static MainWindow() { }

	public MainWindow()
	{

		Client = new SearchClient(new SearchConfig());
		// Shared = new SharedInfo();
		// m_queries = new ConcurrentDictionary<string, SearchQuery>();

		InitializeComponent();

		m_us        = new SemaphoreSlim(1, 1);
		m_us2       = new SemaphoreSlim(1, 1);
		m_mut       = new Mutex();
		DataContext = this;
		SearchStart = default;
		// Results     = new();
		// CurrentQueueItem = new ResultModel();
		// Query            = SearchQuery.Null;

		Queue = new()
		{
			// ResultModel.Nil
			// CurrentQueueItem
			new QueryModel()
		};
		CurrentQuery = Queue[0];

		// QueueSelectedIndex = 0;
		_clipboardSequence = 0;
		m_cts              = new CancellationTokenSource();
		m_ctsu             = new CancellationTokenSource();
		m_ctsm             = new CancellationTokenSource();

		Lb_Engines.ItemsSource  = Engines;
		Lb_Engines2.ItemsSource = Engines;
		Lb_Engines.HandleEnum(Config.SearchEngines);
		Lb_Engines2.HandleEnum(Config.PriorityEngines);

		Logs                = new ObservableCollection<LogEntry>();
		Lv_Logs.ItemsSource = Logs;
		// Lv_Results.ItemsSource = CurrentQueueItem.Results;
		Lb_Queue.ItemsSource = Queue;

		Client.OnResult   += OnResult;
		Client.OnComplete += OnComplete;

#if !DEBUG
		AppDomain.CurrentDomain.UnhandledException += Domain_UHException;
		Application.Current.DispatcherUnhandledException += Dispatcher_UHException;
#endif

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
		m_clipboardHistory       = new();
		Cb_ContextMenu.IsChecked = AppUtil.IsContextMenuAdded;
		// m_resultMap                         = new();
		Image = null;

		Rb_UploadEngine_Catbox.IsChecked    = BaseUploadEngine.Default is CatboxEngine;
		Rb_UploadEngine_Litterbox.IsChecked = BaseUploadEngine.Default is LitterboxEngine;
		Rb_UploadEngine_Pomf.IsChecked      = BaseUploadEngine.Default is PomfEngine;

		// BindingOperations.EnableCollectionSynchronization(Queue, m_lock);
		// BindingOperations.EnableCollectionSynchronization(CurrentQueueItem.Results, m_lock);
		RenderOptions.SetBitmapScalingMode(Img_Preview, BitmapScalingMode.HighQuality);

		Application.Current.Dispatcher.InvokeAsync(CheckForUpdate);

		// ResizeMode         = ResizeMode.NoResize; //todo

		m_pipeBuffer = new List<string>();

		((App) Application.Current).OnPipeMessage += OnPipeReceived;

		m_wndInterop = new WindowInteropHelper(this);

		Cb_SearchFields.ItemsSource   = SearchFields.Keys;
		Cb_SearchFields.SelectedIndex = 0;

		// m_images                      = new();
		AddQueueListener();
		// CurrentQueueItem.PropertyChanged += OnCurrentQueueItemChanged;
		// PropertyChangedEventManager.AddListener(this, this, nameof(CurrentQueueItem) );

		// m_hydrus = new HydrusClient()
		ParseArgs(Args);
		AddHandler(System.Windows.Controls.Validation.ErrorEvent, new RoutedEventHandler(OnValidationRaised));
	}

	#region

	private void OnValidationRaised(object sender, RoutedEventArgs e) { }

	private static readonly ILogger Logger = LoggerFactory
		.Create(builder => builder.AddDebug().AddProvider(new DebugLoggerProvider()))
		.CreateLogger(nameof(MainWindow));

	public static SearchEngineOptions[] Engines { get; } = Enum.GetValues<SearchEngineOptions>();

	private readonly object m_lock = new();

	private readonly WindowInteropHelper m_wndInterop;

	private readonly SemaphoreSlim m_us;
	private readonly SemaphoreSlim m_us2;
	private readonly Mutex         m_mut;
	private readonly List<string>  m_pipeBuffer;

	#endregion

	#region

	public SearchClient Client { get; }

	public SearchConfig Config => Client.Config;

	public SearchQuery? Query
	{
		get { return CurrentQuery?.Query; }
		set
		{
			if (HasQuerySelected) {
				CurrentQuery.Query = value;
			}
		}
	}

	// public ObservableCollection<ResultItem> Results => CurrentQueueItem.Results;

	#endregion

	#region

	// public SharedInfo Shared { get; set; }

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

	private bool m_canReload;

	public bool CanReload
	{
		get => m_canReload;
		set
		{
			if (value == m_canReload) return;
			m_canReload = value;
			OnPropertyChanged();
		}
	}

	private static readonly ConcurrentDictionary<string, string> FileCache = new();

	private readonly ConcurrentDictionary<IDownloadable, string> m_uni;

	// private readonly ConcurrentDictionary<string, SearchQuery> m_queries;

	private BitmapImage? Image
	{
		get => CurrentQuery?.Image;
		set
		{
			if (CurrentQuery != null) {
				CurrentQuery.Image = value;
			}
		}
	}

	private CancellationTokenSource m_cts;
	private CancellationTokenSource m_ctsu;
	private CancellationTokenSource m_ctsm;

	private int m_cntResults;

	// private readonly ConcurrentDictionary<SearchQuery, ObservableCollection<ResultItem>> m_resultMap;

	// private readonly ConcurrentDictionary<SearchQuery, BitmapImage?> m_images;

	#endregion

	#region Queue/Query

	private string? m_input;

	public string? Input
	{
		get => m_input;
		set
		{
			value = value?.CleanString();
			if (value == m_input) return;
			m_input = value;
			OnPropertyChanged();
		}
	}

	[MN]
	private QueryModel m_currentQuery;

	[MN]
	public QueryModel CurrentQuery
	{
		get { return m_currentQuery; }
		set
		{

			if (Equals(value, m_currentQuery) /*|| Query?.ValueString == value*/
			    /* || (String.IsNullOrWhiteSpace(value))*/) return;
			m_currentQuery = value;
			OnPropertyChanged();

		}
	}

	[MNNW(true, nameof(CurrentQuery))]
	public bool HasQuerySelected => CurrentQuery != null;

	public ObservableCollection<QueryModel> Queue { get; }

	// public bool IsQueueInputValid => CurrentQueueItem.IsValid;
	private bool m_isq;

	private void OnCurrentQueryChanged(object? sender, PropertyChangedEventArgs args)
	{
		Input = CurrentQuery?.Value;

		if (!HasQuerySelected /*|| CurrentQueueItem is { HasValue: false }*/ /*|| CurrentQueueItem is not {}*/
		    /* || Query?.ValueString == CurrentQueueItem*/) {
			// SetQueue(String.Empty);

			return;
		}

		m_isq = true;

		if (Tb_Search.Dispatcher.CheckAccess()) {
			ClearSearch();

		}

		Application.Current.Dispatcher.InvokeAsync(async () =>
		{

			var ok = SearchQuery.IsValidSourceType(CurrentQuery?.Value);
			// var ok = true;
			// var ok = true;

			if (ok /*&& !IsInputReady()*/) {
				// Debug.WriteLine($"Updating query: {CurrentQueueItem} | {Query?.ValueString}");
				// await UpdateQueryAsync(CurrentQueueItem);
				//todo
				var ok2 = await UpdateQueryAsync();

				HandleQueryAsync();

				if (ok2.HasValue && !ok2.Value) {
					// var ok3 = RemoveFromQueue(CurrentQueueItem);
					// Tb_Status.Text = $"Removed";
					// Thread.Sleep(TimeSpan.FromSeconds(1));
				}
				else { }
			}

			// Btn_Remove.IsEnabled = ok;
			Btn_Run.IsEnabled = ok;

			if (CurrentQuery is { HasQuery: true } && Url.IsValid(CurrentQuery.Query.Upload)) {
				Tb_Upload.Text = CurrentQuery.Query.Upload;

			}
			else {
				Tb_Upload.Text = null;
			}

			// Tb_Info.Text = CurrentQueueItem.Info;
			// Tb_Status.Text = CurrentQueueItem.Status;
			// Tb_Status2.Text = CurrentQueueItem.Status2;

			if (!ok) {
				ClearQueryControls();
				/*Dispatcher.BeginInvoke(() =>
				{
					CurrentQueueItem.UpdateProperties();
				})*/
				;
			}

			CanReload = !Client.IsRunning && CurrentQuery is { HasQuery: true, Query.IsUploading: false };

		});
		m_isq = false;
		// m_us.Release();
	}

	private async Task<bool?> UpdateQueryAsync()
	{
		if (!await m_us.WaitAsync(TimeSpan.Zero /*, m_cts.Token*/)) {
			return null;
		}

		bool isOk = true;

		if (!HasQuerySelected) {
			isOk = false;
			goto ret;
		}

		if (!CurrentQuery.HasQuery && CurrentQuery.HasValue) {
			isOk = await CurrentQuery.LoadQueryAsync(m_cts.Token);
		}

		if (!isOk) {
			Trace.WriteLine($"Failed to load {CurrentQuery}");
			// Btn_Remove.IsEnabled = true;
			goto ret;
		}

		if (CurrentQuery.HasQuery && !CurrentQuery.Query.IsUploaded) {
			Pb_Status.IsIndeterminate = true;
			Lb_Queue.IsEnabled        = false;
			Btn_Run.IsEnabled         = false;
			isOk                      = await CurrentQuery.UploadAsync(m_ctsu.Token);

			Lb_Upload.Foreground = isOk ? Brushes.Green : Brushes.Red;
			// Img_Upload.Source         = isOk ? AppComponents.accept : AppComponents.exclamation;

			if (!isOk) {
				// Debugger.Break();
			}

			Pb_Status.IsIndeterminate = false;
			Lb_Queue.IsEnabled        = true;
			Btn_Run.IsEnabled         = CurrentQuery.CanSearch;
			Btn_Reload.IsEnabled      = true;
		}

		/*if (!Equals(Lv_Results.ItemsSource, CurrentQueueItem.Results)) {
			Lv_Results.ItemsSource = CurrentQueueItem.Results;

		}*/

		// CurrentQueueItem.PropertyChanged += OnResultModelPropertyChanged;
		// m_image        = CurrentQueueItem.Image;
		// UpdatePreview(CurrentQueueItem.Image);
		/*if (!Equals(Image, CurrentQueueItem.Image)) {
			Image = CurrentQueueItem.Image;

		}*/

		SetPreviewToCurrentQuery();

		if (CurrentQuery.HasQuery) {
			Tb_Upload.Text = CurrentQuery.Query.Upload;

		}
		else {
			Tb_Upload.Text = null;
		}

		ret:
		m_us.Release();

		return isOk;
	}

	private bool RemoveFromQueue(QueryModel old)
	{

		if (!HasQuerySelected || (old.IsPrimitive && !old.HasValue)) {
			return true;
		}

		var i = Queue.IndexOf(old);
		Queue.Remove(old);
		old?.Dispose();

		if (Queue.Count == 0) {
			Queue.Add(new QueryModel());

		}

		if (Queue.Count > 0) {
			// Lb_Queue.SelectedIndex = 0;
			CurrentQuery = Queue[(i) % Queue.Count];
		}
		else { }

		return false;
	}

	/*private void OnResultModelPropertyChanged(object? o, PropertyChangedEventArgs eventArgs)
	{
		if (((Config.AutoSearch && !Client.IsRunning) && !Results.Any()) && CurrentQueueItem.IsPrimitive) {
			Dispatcher.InvokeAsync(RunAsync);
		}
	}*/

	/*
	private async Task UpdateQueryAsync(string query)
	{
		if (query == Query?.ValueString) {
			return;
		}

		/*if (await m_us.WaitAsync(TimeSpan.Zero)) {
			Debug.WriteLine($"blocking");
			return;
		}#1#

		Lb_Queue.IsEnabled = false;
		Btn_Run.IsEnabled  = false;
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

			Tb_Preview.Text = "Rendering preview...";

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
			}#1#

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

		Btn_Run.IsEnabled  = queryExists;
		Lb_Queue.IsEnabled = true;
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
	*/

	private void HandleQueryAsync()
	{

		if ((Config.AutoSearch && !Client.IsRunning) && !CurrentQuery.Results.Any() && CurrentQuery.CanSearch) {
			Dispatcher.InvokeAsync(RunAsync);
		}

		else if (CurrentQuery.Results.Any()) {
			Tb_Status.Text = $"Displaying {CurrentQuery.Results.Count} results";
		}
		else if (CurrentQuery.HasInitQuery && !CurrentQuery.Results.Any()) {
			Tb_Status.Text = $"Search ready";

		}
		else {
			Tb_Status.Text = CurrentQuery.Status;

		}

		Btn_Run.IsEnabled = CurrentQuery.CanSearch;
		// Tb_Info.Text      = CurrentQuery.Info;
		// Tb_Status.Text    = CurrentQueueItem.Status;
		Tb_Status2.Text = CurrentQuery.Status2;
		CurrentQuery.UpdateProperties();
		// OnPropertyChanged(nameof(Results));
	}

	private void AddToQueue(IReadOnlyList<string> files)
	{
		if (!files.Any()) {
			return;
		}

		int c = 0;

		foreach (var s in files) {

			if (SearchQuery.IsValidSourceType(s) && !Queue.Any(x => x.Value == s)) {
				Queue.Add(new QueryModel(s));
				Debug.WriteLine($"Added {s}");

				c++;
			}

			if (!CurrentQuery.HasValue) {
				SetQueue(s);

			}
		}

		/*if (!CurrentQueueItem.HasValue && files.Any()) {
			var ff = files[0];
			// CurrentQueueItem = new ResultModel(ff); //todo
			SetQueue(ff);
			// Debug.WriteLine($"cqi {ff}");
			// Lv_Queue.SelectedItems.Add(ff);
		}*/

		Tb_Status2.Text = $"Added {c} items to queue";
	}

	private void AdvanceQueue(int i = 1)
	{
		QueryModel next;

		if (Queue.Count == 0) {
			next = new QueryModel();
		}
		else next = Queue[(Queue.IndexOf(CurrentQuery) + i) % Queue.Count];

		CurrentQuery = next;
	}

	private void AddQueueListener()
	{
		PropertyChangedEventManager.AddHandler(this, OnCurrentQueryChanged, nameof(CurrentQuery));
	}

	private void RemoveQueueListener()
	{
		PropertyChangedEventManager.RemoveHandler(this, OnCurrentQueryChanged, nameof(CurrentQuery));
	}

	#endregion

	#region Clipboard

	public bool UseClipboard
	{
		get => Config.Clipboard;
		set => Config.Clipboard = m_cbDispatch.IsEnabled = value;
	}

	private readonly DispatcherTimer m_cbDispatch;

	private readonly List<string> m_clipboardHistory;

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
			// CurrentQueueItem = fn;
			SetQueue(fn);
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

				if ( /*!IsInputReady() && */ /*!Queue.Any(x => x.Value == txt) &&*/ !m_clipboardHistory.Contains(txt)
				    /*&& SearchQuery.IsValidSourceType(txt)*/) {
					m_clipboardHistory.Add(txt);
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
			rg = rg.Where(x => !m_clipboardHistory.Contains(x)).ToArray();
			AddToQueue(rg);

			m_clipboardHistory.AddRange(rg);

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

		CanReload = false;

		Lb_Queue.IsEnabled = false;

		Btn_Remove.IsEnabled = false;
		// ClearResults();

		SearchStart = DateTime.Now;

		m_cntResults      = 0;
		Btn_Run.IsEnabled = false;

		Tb_Status.Text = "Initiating search...";
		bool? isOk = CurrentQuery?.HasInitQuery;

		if (isOk.HasValue && !isOk.Value) {
			isOk = await UpdateQueryAsync();

		}

		if (isOk.HasValue && !isOk.Value) {
			Tb_Status.Text       = $"Could not load query";
			Lb_Queue.IsEnabled   = true;
			Btn_Run.IsEnabled    = true;
			Btn_Remove.IsEnabled = true;
			return;
		}

		// HandleQueryAsync();
		try {
			var r = await Client.RunSearchAsync(Query, reload: false, token: m_cts.Token,
			                                    scheduler: TaskScheduler.FromCurrentSynchronizationContext());

		}
		catch (Exception e) {
			// Debugger.Break();
			Debug.WriteLine($"{e.Message}");
		}
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

		Btn_Remove.IsEnabled = true;
		Btn_Run.IsEnabled    = CurrentQuery.HasValue;
		// m_resultMap[Query] = Results;
		CurrentQuery.UpdateProperties();
		CanReload = true;
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

		var sri1 = SearchResultItem.GetRaw(result);

		CurrentQuery.Results.Add(new ResultItem(sri1, sri1.Root.Engine.Name));

		foreach (SearchResultItem sri in allResults) {
			CurrentQuery.Results.Add(new ResultItem(sri, $"{sri.Root.Engine.Name} #{++i}"));
			int j = 0;

			foreach (var ssri in sri.Children) {
				var srir = new ResultItem(ssri, $"{ssri.Root.Engine.Name} #{i}.{++j}")
				{
					IsSister = true
				};
				CurrentQuery.Results.Add(srir);

			}
		}

	}

	#endregion

	#region

	private readonly DispatcherTimer m_trDispatch;

	private async void IdleDispatchAsync(object? sender, EventArgs e)
	{
		// Dispatcher.InvokeAsync(UpdateItem2);
		/*if (Client.IsRunning) {
			return;
		}
		if (CurrentQueueItem.IsPrimitive) {
			OnCurrentQueueItemChanged(sender, null);

		}*/

		// await UpdateItem();

		/*var ok = SearchQuery.IsValidSourceType(CurrentQueueItem?.Value);

		Btn_Run.IsEnabled = ok;

		if (ok) { }*/

		Debug.WriteLine($"{CanReload} | {CurrentQuery?.Value} | {CurrentResult}");
	}

	#endregion

	#region

	private void ReloadToken()
	{
		m_cts  = new();
		m_ctsu = new();
		m_ctsm = new();
	}

	private void Cancel()
	{
		m_cts.Cancel();
		m_ctsu.Cancel();
		m_ctsm.Cancel();
		// Pb_Status.Foreground = new SolidColorBrush(Colors.Red);
		Pb_Status.IsIndeterminate = false;
	}

	private void Restart(bool full = false)
	{
		Cancel();
		Dispose(full);
		ClearResults(full);

		// CurrentQueueItem = new ResultModel();
		ReloadToken();
	}

	private void ClearQueryControls()
	{
		Image = null;
		// m_images.TryRemove(Query, out var img);
		Img_Preview.Source = null;
		Img_Preview.UpdateLayout();
		Tb_Status.Text = String.Empty;
		// CurrentQueueItem = new ResultModel();
		// Tb_Info.Text    = String.Empty;
		Tb_Status2.Text = String.Empty;

		Tb_Upload.Text            = String.Empty;
		Pb_Status.IsIndeterminate = false;
		Tb_Preview.Text           = string.Empty;
		Lb_Queue.IsEnabled        = true;
	}

	private void ClearResults(bool full = false)
	{
		m_cntResults = 0;

		if (full) {
			CurrentQuery.ClearResults();
			// ClearQueryControls();
		}

		// Btn_Run.IsEnabled = CurrentQueueItem.CanSearch;
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
		Lb_Upload.Foreground = Brushes.White;
		// SetQueue(null);
		// CurrentQueueItem = String.Empty;
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
	}

	public void Dispose(bool full)
	{

		if (full) {
			// Client.Dispose();
			// Query.Dispose();
			// Query = SearchQuery.Null;
			CurrentQuery?.Dispose();

			// Queue.Clear();
			// QueueSelectedIndex = 0;

			ClearQueue();

			foreach (var kv in m_uni) {
				kv.Key.Dispose();
			}

			m_uni.Clear();
			m_clipboardHistory.Clear();

			/*foreach (var r in Results) {
				r.Dispose();
			}

			Results.Clear();*/

			/*foreach ((SearchQuery key, ObservableCollection<ResultItem> value) in m_resultMap) {
				key.Dispose();
				value.Clear();
			}

			m_resultMap.Clear();*/

			Image              = null;
			Img_Preview.Source = Image;
			Img_Preview.UpdateLayout();

			// m_images.Clear();

			try {
				m_us.Release();
				m_us2.Release();
			}
			catch (Exception e) {
				Debug.WriteLine($"{e.Message}");
			}
		}

		m_cts.Dispose();
		m_ctsu.Dispose();
		m_ctsm.Dispose();
	}

	#endregion

	#region

	public DateTime SearchStart { get; private set; }

	#endregion

	#region

	private static async Task<string?> CacheOrGetAsync(Url v, CancellationToken c = default)
	{

		try {
			if (FileCache.TryGetValue(v, out string? async)) {
				return async;
			}

			var rg = await v.GetBytesAsync(cancellationToken: c);
			var fn = v.GetFileName();

			var s = Path.Combine(Path.GetTempPath(), fn);

			await File.WriteAllBytesAsync(s, rg, c);

			FileCache[v] = s;
			Debug.WriteLine($"Cached {v} to {s}");
			return s;
		}
		catch (Exception e) {
			return null;
		}
	}

	private async Task DownloadResultAsync(IDownloadable uri)
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
					CurrentQuery.Results.Insert(CurrentQuery.Results.IndexOf(ri) + 1 + i, rii);
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
				CurrentQuery.Results.Insert(CurrentQuery.Results.IndexOf(cri) + 1 + i, rii);
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
		bool r   = Btn_Run.IsEnabled,
		     re  = CanReload,
		     rem = Btn_Remove.IsEnabled,
		     q   = Lb_Queue.IsEnabled;

		Btn_Run.IsEnabled    = false;
		CanReload            = false;
		Btn_Remove.IsEnabled = false;
		Lb_Queue.IsEnabled   = false;

		Btn_Filter.IsEnabled = false;

		Pb_Status.IsIndeterminate = true;
		Tb_Status.Text            = "Filtering";

		var cb = new ConcurrentBag<ResultItem>(CurrentQuery.Results);
		int c  = 0;

		await Parallel.ForEachAsync(cb, async (item, token) =>
		{
			var f = await FilterResultAsync(item, token);

			if (!f) {
				Dispatcher.Invoke(() => CurrentQuery.Results.Remove(item));
				c++;
			}

			return;
		});

		Debug.WriteLine("continuing");
		Tb_Status.Text            = $"Filtered {c}";
		Pb_Status.IsIndeterminate = false;
		Btn_Filter.IsEnabled      = true;
		Btn_Run.IsEnabled         = r;
		CanReload                 = re;
		Lb_Queue.IsEnabled        = q;
		Btn_Remove.IsEnabled      = rem;

	}

	private async Task<bool> FilterResultAsync(ResultItem item, CancellationToken token)
	{
		if (item.IsLowQuality) {

			return false;
		}

		var res = await item.GetResponseAsync(token);

		if (res is { ResponseMessage.IsSuccessStatusCode: false }) {
			Debug.WriteLine($"removing {item}");
			return false;
		}
		else { }

		return true;

	}

	private async Task EnqueueResultAsync(IDownloadable uri)
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
		var idx = FindResultIndex(r => r.Result.Root.Engine == eng);
		var fi  = idx;

		for (int i = CurrentQuery.Results.Count - 1; i >= 0; i--) {
			if (CurrentQuery.Results[i].Result.Root.Engine == eng) {
				CurrentQuery.Results.RemoveAt(i);
			}
		}

		Pb_Status.IsIndeterminate = true;
		var result = await eng.GetResultAsync(Query, m_cts.Token);
		AddResult(result);
		Tb_Status.Text            = $"{eng.Name} → {result.Results.Count}";
		Pb_Status.IsIndeterminate = false;

		for (int i = 0; i < CurrentQuery.Results.Count; i++) {
			var cr = CurrentQuery.Results[i];

			if (cr.Result.Root.Engine == eng) {
				CurrentQuery.Results.Move(i, idx++);

			}
		}

		Lv_Results.ScrollIntoView(CurrentQuery.Results[fi]);
	}

	#endregion

	#region

	public static readonly string[] Args = Environment.GetCommandLineArgs();

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
				SetQueue(inp);

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

	#endregion

	#region

	public bool IsSearching { get; private set; }

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

	internal static readonly string Placeholder = "-";

	private void ClearSearch()
	{
		if (Tb_Search.Text != null) {
			Tb_Search.Text = null;
		}

		CurrentQuery.RestoreResults();
		// Lv_Results.ItemsSource = CurrentQueueItem.Results; //todo
		IsSearching = false;

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
		var eventArgs = new PropertyChangedEventArgs(propertyName);
		PropertyChanged?.Invoke(this, eventArgs);
		// Debug.WriteLine($"{this} :: {eventArgs.PropertyName}");
	}

	/*public delegate bool PreviewChangedCallback(ResultItem? ri);

	public event PreviewChangedCallback? PreviewChanged;*/

	#region

	private void SetPreview(IGuiImageSource igs)
	{
		/*if (ri.Load()) {
			UpdatePreview(ri.Image);
		}*/

		Application.Current.Dispatcher.InvokeAsync(async () =>
		{
			if (Img_Preview.Source != null) {
				if (Img_Preview.Source.Dispatcher != null) {
					if (!Img_Preview.Source.Dispatcher.CheckAccess()) {
						return;
					}

				}
			}
		});
		/*if (!await m_us2.WaitAsync(TimeSpan.Zero)) {
		return;
	}*/
		var load = igs.LoadImage();

		if (!load) {
			SetPreviewToCurrentQuery();
			return;
		}

		string name = igs is INamed n ? n.Name : Placeholder;
		string n2;

		if (igs.IsThumbnail.HasValue) {
			if (igs.IsThumbnail.Value) {
				n2 = "thumbnail";

			}
			else {
				n2 = "full res";
			}

		}
		else {
			n2 = Placeholder;
		}

		string name2 = null;

		if (igs is ResultItem rri) {

			if (rri.IsSister) {
				/*var grp=CurrentQueueItem.Results.GroupBy(x => x.Result.Root);

				foreach (IGrouping<SearchResult, ResultItem> items in grp) {
					// var zz=items.GroupBy(y => y.Result.Root.AllResults.Where(yy => yy.Sisters.Contains(y.Result)));

				}*/
				var p = FindParent(rri);
				Debug.WriteLine($"{p}");
				name  = p.Name;
				name2 = $"(parent)";
			}
		}

		/*Tb_Preview.Dispatcher.Invoke(() =>
		{

		});*/

		Img_Preview.Source = igs.Image;

		// Debug.WriteLine($"updated image {ri.Image}");
		// PreviewChanged?.Invoke(ri);

		/*if (ri.Image != null) {
				using var bmp1 = CurrentQueueItem.Image.BitmapImage2Bitmap();
				using var bmp2 = ri.Image.BitmapImage2Bitmap();
				mse = AppUtil.CompareImages(bmp1, bmp2,1);

			}*/

		Tb_Preview.Text = $"Preview: {name} ({n2}) {name2}";

		// m_us2.Release();

	}

	private void SetPreviewToCurrentQuery()
	{
		SetPreview(CurrentQuery);
		// UpdatePreview(m_image);
		Tb_Preview.Text = $"Preview: (query)";
	}

	#endregion

	private IEnumerable<ResultItem> FindSisters(ResultItem r)
	{
		foreach (ResultItem resultItem in CurrentQuery.Results) {
			foreach (var item in resultItem.Result.Children) {
				if (resultItem.Result == item) {
					yield return resultItem;
				}
			}
		}
	}

	private ResultItem? FindParent(ResultItem r)
	{
		foreach (ResultItem item in CurrentQuery.Results) {
			if (item.Result.Children.Contains(r.Result)) {
				return item;
			}
		}

		return null;
	}

	private void SetRenderMode()
	{
		// todo
		//https://stackoverflow.com/questions/23075609/wpf-mediaelement-video-freezes
		try {
			var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
			var hwndTarget = hwndSource.CompositionTarget;
			hwndTarget.RenderMode = RenderMode.SoftwareOnly;
		}
		catch (Exception ex) {
			Debugger.Break();
			Console.WriteLine(ex);
		}
	}

}