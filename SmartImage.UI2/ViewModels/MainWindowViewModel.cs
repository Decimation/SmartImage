// Author: Deci | Project: SmartImage.UI2 | Name: MainWindowViewModel.cs
// Date: 2025/08/06 @ 11:08:36

#region Global usings

global using IAvImage = Avalonia.Media.IImage;

#endregion

using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using ReactiveUI.Primitives;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Results;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input.Platform;
using Avalonia.Logging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using SmartImage.Lib.Engines.Search.Base;
using SmartImage.Lib.Engines.Upload.Base;
using SmartImage.Lib.Images;
using SmartImage.Lib.Images.Alloc;
using SmartImage.Lib.Model;
using SmartImage.UI2.Controls;

namespace SmartImage.UI2.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{

	private static readonly ConditionalWeakTable<IAllocImage, IAvImage> s_imageCache = new();

	private readonly DispatcherTimer m_cbDispatch;

	private CancellationTokenSource m_cts;

	public UploadEngineOption[] UploadEngineOptions => ValidUploadOptions;

	public ObservableCollection<ReactiveEnumOption<SearchEngineOptions>> SearchEngineItems { get; }

	public ObservableCollection<ReactiveEnumOption<SearchEngineOptions>> PriorityEngineItems { get; }

	public ObservableCollection<IResultItem> Items { get; } = [];

	public IUploadEngine UploadEngine { get; private set; }

#region Rx Properties

	public SearchClient Client
	{
		get;
		private set => this.RaiseAndSetIfChanged(ref field, value);
	}

	public SearchConfig Config
	{
		get;
		set => this.RaiseAndSetIfChanged(ref field, value);
	}

	[Reactive]
	public SearchQuery Query
	{
		get;
		set => this.RaiseAndSetIfChanged(ref field, value);
	}

	[MNNW(true, nameof(Query.Upload.Url))]
	public bool IsReady
	{
		get;
		set => this.RaiseAndSetIfChanged(ref field, value);
	}

	[Reactive]
	public string? Input
	{
		get;
		set => this.RaiseAndSetIfChanged(ref field, value);
	}

	public IAvImage Image
	{
		get;
		set => this.RaiseAndSetIfChanged(ref field, value);
	}

	public Url Url
	{
		get;
		private set => this.RaiseAndSetIfChanged(ref field, value);
	}

	public double Progress
	{
		get;
		set => this.RaiseAndSetIfChanged(ref field, value);
	}

	public IResultItem SelectedItem
	{
		get;
		set => this.RaiseAndSetIfChanged(ref field, value);
	}

#endregion


#region Rx Commands

	public ReactiveCommand<RxVoid, bool> UploadCommand { get; }

	public ReactiveCommand<RxVoid, RxVoid> SearchCommand { get; }

	public ReactiveCommand<RxVoid, RxVoid> ClearCommand { get; }

#endregion

	public MainWindowViewModel()
	{
		// todo
		/*var loader = ImageLoaderPipelineBuilder.RamCached(new MemoryImageCacheOptions(){})
		                                       .UseDecoder(new BitmapDecoder(){})
		                                       .UseTransport(new D())
		                                       .UseMemoryCache(m_memoryImageCache)
		                                       .Build();
		ImageLoader.AsyncImageLoader = loader;*/

		/* Fields */

		Config = new SearchConfig();
		Client = new SearchClient(Config);
		m_cts  = new CancellationTokenSource();

		SearchEngineItems =
		[
			.. ValidSearchOptions.Select(seo => new ReactiveEnumOption<SearchEngineOptions>(Config, seo, nameof(Config.SearchEngines)))
		];

		PriorityEngineItems =
		[
			.. ValidPriorityOptions.Select(seo => new ReactiveEnumOption<SearchEngineOptions>(Config, seo, nameof(Config.PriorityEngines)))
		];


		/* Clipboard */

		m_cbDispatch = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, ClipboardTick);

		var cbChanged = Config.WhenPropertyChanged(static cfg => cfg.Clipboard, false, null);

		ObservableExtensions.Subscribe(cbChanged, value =>
		{
			if (value.Value) {
				m_cbDispatch.Start();
			}
			else {
				m_cbDispatch.Stop();
			}
		});

		/* Upload */

		var canUpload = this.WhenAnyValue(static mwvm => mwvm.Input, InputPredicate);

		UploadCommand = ReactiveCommand.CreateFromTask(UploadInputAsync, canUpload);

		var isUploaded = Observable.Switch(LinqExtensions.Select(this.WhenAnyValue(static mwvm => mwvm.Query),
		                                                         static q => q?.WhenAnyValueUnsafe(static y => y.IsUploaded)
		                                                                     ?? Observable.Return(false)));

		/* Search */

		var canSearch = Observable.CombineLatest(canUpload, isUploaded, static (input, uploaded) => input && uploaded);

		SearchCommand = ReactiveCommand.CreateFromTask(RunSearchAsync, canSearch);

		ClearCommand = ReactiveCommand.CreateFromTask(ClearAsync);

		/* UE */

		var ueChanged = Config.WhenValueChanged(static cfg => cfg.UploadEngine, false, null);

		ObservableExtensions.Subscribe(ueChanged, value =>
		{
			UploadEngine?.Dispose();
			UploadEngine = BaseUploadEngine.GetUploadEngine(value);
		});

		/* Selected item */

		var selectedItemCmd = ReactiveCommand.CreateFromTask<IResultItem>(SelectedItemAsync);

		ObservableMixins.WhereNotNull(this.WhenAnyValue(static x => x.SelectedItem))
		                .InvokeCommand(selectedItemCmd);


	}

	private static bool InputPredicate(string? x)
	{
		return AllocImageStream.IsValidSourceType(x);
	}

	[RelayCommand]
	public async Task SelectedItemAsync(IResultItem item)
	{
		if (item is ScannedResultItem { AllocImage: { HasImage: true } } scnItem) {
			// Image = Bitmap.DecodeToWidth(scnItem.AllocImage.GetSource(), scnItem.AllocImage.Image.Width);
		}


		if (item is SearchResultItem { HasThumbnail: true } sri) { }

		Image = GetFromCache(item);
	}

	private IAvImage GetFromCache(IResultItem item)
	{
		// todo
		if (item is not IAllocImageView<IAllocImage> { AllocImage: { HasImage: true } allocImg } allocImgView) {
			var hasQueryImg = s_imageCache.TryGetValue(Query.AllocImage, out var queryImg);
			return queryImg;
		}


		if (s_imageCache.TryGetValue(allocImg, out var cached)) {
			return cached;
		}


		using var stream = allocImg.GetSource();
		var       bitmap = new Bitmap(stream);

		s_imageCache.AddOrUpdate(allocImg, bitmap);

		return bitmap;
	}

	[RelayCommand]
	public async Task LoadItemAsync(IResultItem item)
	{
		if (item is IScannableItem { } scannable) {
			var ok = await scannable.ScanAsync(m_cts.Token);

			if (ok) {
				scannable.TryCalculateSimilarity(Query.AllocImage);
				Items.AddOrInsertRange(scannable.ScannedItems, Items.IndexOf((IResultItem) scannable) + 1);
			}

		}
	}

	[RelayCommand]
	public async Task HashItemAsync(IResultItem item)
	{
		if (item is ScannedResultItem { HasHash: false } scnItem) {
			scnItem.CalculateHash();
		}

		if (item is { HasHash: true, HasSimilarity: false }) {
			var ok = item.TryCalculateSimilarity(Query.AllocImage);
			this.RaisePropertyChanged(nameof(SelectedItem.Similarity));
		}
	}

	[RelayCommand]
	public async Task GalleryDLItemAsync(object item)
	{

		if (item is IScannableItem { HasScannedItems: false } sri) {
			var ch      = Channel.CreateUnbounded<Url>();
			var gdlTask = ImageScanner.RunGalleryDLAsync(((IUrl) sri).Url, ch.Writer, m_cts.Token);

			while (await ch.Reader.WaitToReadAsync(m_cts.Token)) {
				var res = await ch.Reader.ReadAsync(m_cts.Token);

				if (res is not null) {
					var resAi = await AllocImageStream.FromSourceAsync(res, ct: m_cts.Token);

					if (resAi is { HasImage: true }) {
						var scn = new ScannedResultItem(sri, resAi);
						sri.ScannedItems.Add(scn);
						sri.TryCalculateSimilarity(Query.AllocImage);
						this.RaisePropertyChanged(nameof(SelectedItem.Similarity));
						Items.Insert(Items.IndexOf(item) + 1, scn);

					}

				}
			}

			await gdlTask;

		}
	}

	[RelayCommand]
	public async Task<bool> UploadInputAsync()
	{
		Query = await SearchQuery.TryCreateAsync(Input?.Trim('\"'));
		var isReady = await Query.TryUploadAsync(UploadEngine);

		if (isReady) {
			Trace.Assert(Query.Upload != null);

			// IsReady = Query.IsUploaded;
			Url   = Query.Upload.Url;
			Image = Bitmap.DecodeToWidth(Query.AllocImage.GetSource(), Query.AllocImage.Image.Width);
			var ai = Query.AllocImage;
			s_imageCache.AddOrUpdate(Query.AllocImage, Image);

		}

		return isReady;
	}


	// [RelayCommand]
	public async Task RunSearchAsync()
	{
		try {

			var searchTask = Client.RunSearchAsync(Query, m_cts.Token);

			var count = Client.Engines.Count();
			int c     = 0;

			while (await Client.ResultChannel.Reader.WaitToReadAsync(m_cts.Token)) {
				var res = await Client.ResultChannel.Reader.ReadAsync(m_cts.Token);

				foreach (var item in res.Results) {
					Items.Add((item));
				}

				Progress = (++c / (double) count) * 100d;
			}

			await searchTask;
		}
		catch (OperationCanceledException e) {
			// ...
		}
		catch (Exception e) {
			// ...
		}

	}

	[RelayCommand]
	public async Task CancelAsync()
	{
		await m_cts.CancelAsync();
		m_cts.Dispose();
		m_cts = new CancellationTokenSource();
	}

	public async Task ClearAsync()
	{
		Items.Clear();
		Input = null;

		Query?.Dispose();
		Query = null;

		Url = new Url();

		Client.Dispose();
		Client = new SearchClient(Config);

		Image = null;

		if (Config.Clipboard) {
			m_cbDispatch.Start();
		}
	}

	private async void ClipboardTick(object? sender, EventArgs args)
	{
		try {
			if (Application.Current == null || IsReady) {
				return;
			}

			var tl        = Application.Current.GetTopLevel();
			var clipboard = tl?.Clipboard;

			if (clipboard == null)
				return;

			var clipFile = await clipboard.TryGetFileAsync();


			if (clipFile?.TryGetLocalPath() is { } lp && InputPredicate(lp)) {
				Input = lp;
				m_cbDispatch.Stop();

			}

			var clipText = await clipboard.TryGetTextAsync();

			if (InputPredicate(clipText)) {
				Input = clipText;
				m_cbDispatch.Stop();

			}
		}
		catch (Exception e) {
			Logger.Sink?.Log(LogEventLevel.Error, nameof(ClipboardTick), this, "");
			throw; // TODO handle exception
		}
	}

}