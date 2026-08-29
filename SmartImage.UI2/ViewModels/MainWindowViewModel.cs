// Author: Deci | Project: SmartImage.UI2 | Name: MainWindowViewModel.cs
// Date: 2025/08/06 @ 11:08:36

using AsyncImageLoader;
using AsyncImageLoader.Loaders;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using ReactiveUI.Primitives;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Results;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;

// using System.Reactive.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Skia.Helpers;
using Avalonia.Threading;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Primitives;
using SmartImage.Lib.Engines.Search.Base;
using SmartImage.Lib.Engines.Upload.Base;
using SmartImage.Lib.Images;
using SmartImage.Lib.Images.Alloc;
using SmartImage.Lib.Model;
using IImage = Avalonia.Media.IImage;
using SmartImage.UI2.Controls;

namespace SmartImage.UI2.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{

	public static readonly UploadEngineOption[] ValidUploadOptions = Enum.GetValues<UploadEngineOption>()
	                                                                     .Where(static e => e != UploadEngineOption.None
	                                                                                        && !BaseUploadEngine.ObsoleteUploadEngines.Contains(e))
	                                                                     .ToArray();

	public static readonly SearchEngineOptions[] ValidSearchOptions = Enum.GetValues<SearchEngineOptions>()
	                                                                      .Where(static e => e is not (SearchEngineOptions.Auto or SearchEngineOptions.None)
	                                                                                         && !e.HasFlag(SearchEngineOptions.Obsolete))
	                                                                      .ToArray();

	public static readonly SearchEngineOptions[] ValidPriorityOptions = Enum.GetValues<SearchEngineOptions>()
	                                                                        .Where(static e => e != SearchEngineOptions.None
	                                                                                           && !e.HasFlag(SearchEngineOptions.Obsolete))
	                                                                        .ToArray();

	public UploadEngineOption[] UploadEngineOptions { get; } = ValidUploadOptions;

	public ObservableCollection<ReactiveEnumOption<SearchEngineOptions>> SearchEngineItems { get; }

	public ObservableCollection<ReactiveEnumOption<SearchEngineOptions>> PriorityEngineItems { get; }

	// Keyed by instance identity so entries are collected alongside their AllocImageStream
	// rather than needing an explicit cache-invalidation/eviction policy.

	public ObservableCollection<IResultItem> Items { get; } = [];

	private static readonly ConditionalWeakTable<IAllocImage, IImage> s_cache = new();

	private readonly DispatcherTimer m_cbDispatch;

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

	public SearchQuery Query
	{
		get;
		set => this.RaiseAndSetIfChanged(ref field, value);
	}

	public IUploadEngine UploadEngine { get; private set; }

	[MNNW(true, nameof(Query.Upload.Url))]
	public bool IsReady
	{
		get;
		set => this.RaiseAndSetIfChanged(ref field, value);
	}

	public string? Input
	{
		get;
		set => this.RaiseAndSetIfChanged(ref field, value);
	}

	public IImage Image
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

	public CancellationTokenSource TokenSource { get; private set; }

#region

	public ReactiveCommand<RxVoid, bool> UploadCommand { get; }

	public ReactiveCommand<RxVoid, RxVoid> SearchCommand { get; }

	public ReactiveCommand<RxVoid, RxVoid> ClearCommand { get; }

#endregion

	public MainWindowViewModel()
	{
		m_cbDispatch = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, ClipboardTick);
		
		Config      = new SearchConfig();
		Client      = new SearchClient(Config);
		TokenSource = new CancellationTokenSource();

		SearchEngineItems = new ObservableCollection<ReactiveEnumOption<SearchEngineOptions>>(
			ValidSearchOptions.Select(seo => new ReactiveEnumOption<SearchEngineOptions>(Config, seo, nameof(Config.SearchEngines))));

		PriorityEngineItems = new ObservableCollection<ReactiveEnumOption<SearchEngineOptions>>(
			ValidPriorityOptions.Select(seo => new ReactiveEnumOption<SearchEngineOptions>(Config, seo, nameof(Config.PriorityEngines))));

		Config.PropertyChanged += OnChangedEvent;

		var canUpload = this.WhenAnyValue(static mwvm => mwvm.Input, InputPredicate);

		UploadCommand = ReactiveCommand.CreateFromTask(UploadInputAsync, canUpload);

		var isUploaded = Observable.Switch(LinqExtensions.Select(this.WhenAnyValue(static mwvm => mwvm.Query),
		                                                         static q => q?.WhenAnyValue(static y => y.IsUploaded)
		                                                                     ?? Observable.Return(false)));

		var canSearch = Observable.CombineLatest(canUpload, isUploaded, static (input, uploaded) => input && uploaded);

		SearchCommand = ReactiveCommand.CreateFromTask(RunSearchAsync, canSearch);

		ClearCommand = ReactiveCommand.CreateFromTask(ClearAsync);

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

		var ueChanged = Config.WhenValueChanged(static cfg => cfg.UploadEngine, false, null);

		ObservableExtensions.Subscribe(ueChanged, value =>
		{
			UploadEngine?.Dispose();
			UploadEngine = BaseUploadEngine.GetUploadEngine(value);
		});

		var selectedItemCmd = ReactiveCommand.CreateFromTask<IResultItem>(SelectedItemAsync);

		ObservableMixins.WhereNotNull(this.WhenAnyValue(static x => x.SelectedItem))
		                .InvokeCommand(selectedItemCmd);


	}

	private static bool InputPredicate(string? x)
	{
		return AllocImageStream.IsValidSourceType(x?.ToString());
	}

	private void OnChangedEvent(object? sender, PropertyChangedEventArgs args)
	{
		/*switch (args.PropertyName) {
			case nameof(Config.Clipboard):
				if (Config.Clipboard) {
					m_cbDispatch.Start();
				}
				else {
					m_cbDispatch.Stop();
				}

				break;
		}*/
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

	private IImage GetFromCache(IResultItem item)
	{

		if (item is not IAllocImageView<IAllocImage> {AllocImage: {HasImage: true} allocImg} allocImgView) {
			var hasQueryImg= s_cache.TryGetValue(Query.AllocImage, out var queryImg);
			return queryImg;
		}
		

		if (s_cache.TryGetValue(allocImg, out var cached)) {
			return cached;
		}

		using var stream = allocImg.GetSource();
		var       bitmap = new Bitmap(stream);

		s_cache.AddOrUpdate(allocImg, bitmap);

		return bitmap;
	}

	[RelayCommand]
	public async Task LoadItemAsync(IResultItem item)
	{
		if (item is IScannableItem { } scannable) {
			var ok = await scannable.ScanAsync(TokenSource.Token);

			if (ok) {
				Items.AddOrInsertRange(scannable.ScannedItems, Items.IndexOf((IResultItem) scannable) + 1);
			}

		}
	}

	[RelayCommand]
	public async Task HashItemAsync(IResultItem item)
	{
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
			var gdlTask = ImageScanner.RunGalleryDLAsync(((IUrl) sri).Url, ch.Writer, TokenSource.Token);

			while (await ch.Reader.WaitToReadAsync(TokenSource.Token)) {
				var res = await ch.Reader.ReadAsync(TokenSource.Token);

				if (res is not null) {
					var resAi = await AllocImageStream.FromSourceAsync(res, ct: TokenSource.Token);

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
		Query = await SearchQuery.TryCreateAsync(Input.Trim('\"'));
		var isReady = await Query.TryUploadAsync(UploadEngine);

		if (isReady) {
			Trace.Assert(Query.Upload != null);

			// IsReady = Query.IsUploaded;
			Url   = Query.Upload.Url;
			Image = Bitmap.DecodeToWidth(Query.AllocImage.GetSource(), Query.AllocImage.Image.Width);
			var ai = Query.AllocImage;
			s_cache.AddOrUpdate(Query.AllocImage, Image);

		}

		return isReady;
	}


	// [RelayCommand]
	public async Task RunSearchAsync()
	{
		try {

			var r = Client.RunSearchAsync(Query, TokenSource.Token);

			var max = Client.Engines.Count();
			int c   = 0;

			while (await Client.ResultChannel.Reader.WaitToReadAsync(TokenSource.Token)) {
				var res = await Client.ResultChannel.Reader.ReadAsync(TokenSource.Token);

				foreach (var item in res.Results) {
					Items.Add((item));
				}

				Progress = (++c / (double) max) * 100d;
			}

			await r;
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
		await TokenSource.CancelAsync();
		TokenSource.Dispose();
		TokenSource = new CancellationTokenSource();
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