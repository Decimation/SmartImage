// Author: Deci | Project: SmartImage.UI2 | Name: MainWindowViewModel.cs
// Date: 2025/08/06 @ 11:08:36

using AsyncImageLoader;
using AsyncImageLoader.Loaders;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Novus.Win32;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Results;
using SmartImage.UI2.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Skia.Helpers;
using Avalonia.Threading;
using DynamicData.Binding;
using ReactiveUI;
using SmartImage.Lib.Images.Alloc;

namespace SmartImage.UI2.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{

	public ObservableCollection<IResultItem> Items { get; } = [];

	public SearchClient Client { get; }

	public SearchConfig Config { get; }

	public SearchQuery Query { get; set; }

	[MNNW(true, nameof(Query.Upload.Url))]
	public bool IsReady
	{
		get;
		set => this.RaiseAndSetIfChanged(ref field, value);
	}

	public string Input
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

	private readonly DispatcherTimer m_cbDispatch;

#region

	public ReactiveCommand<Unit, Unit> UploadCommand { get; }

	public ReactiveCommand<Unit, Unit> SearchCommand { get; }

	public ReactiveCommand<Unit, Unit> ClearCommand { get; }

#endregion

	public MainWindowViewModel()
	{
		m_cbDispatch = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, ClipboardTick);

		Config      = new SearchConfig();
		Client      = new SearchClient(Config);
		TokenSource = new CancellationTokenSource();

		Config.PropertyChanged += OnChangedEvent;

		var canUpload = this.WhenAnyValue(x => x.Input, InputPredicate);
		UploadCommand = ReactiveCommand.CreateFromTask(UploadInputAsync, canUpload);

		var canSearch = this.WhenAnyValue(x => x.IsReady);
		SearchCommand = ReactiveCommand.CreateFromTask(RunSearchAsync, canSearch);

		/*var canScan = this.WhenAnyValue(x => x.SelectedItem, (SearchResultItem s) => { return s.HasScannedItems; });
		SearchCommand = ReactiveCommand.CreateFromTask(RunSearchAsync, isUp);*/

		ClearCommand = ReactiveCommand.CreateFromTask(ClearAsync);
		
		var cbChanged=Config.WhenPropertyChanged(x=>x.Clipboard, false, null);
		cbChanged.Subscribe(value =>
		{
			if (value.Value) {
				m_cbDispatch.Start();
			}
			else {
				m_cbDispatch.Stop();
			}
		});
	}

	private static bool InputPredicate(string x)
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
	public async Task LoadItemAsync()
	{
		if (SelectedItem is IScannableItem { } scannable) {
			var ok = await scannable.ScanAsync(TokenSource.Token);

			if (ok) {
				Items.AddOrInsertRange(scannable.ScannedItems, Items.IndexOf((IResultItem) scannable));
			}

		}
	}

	[RelayCommand]
	public async Task HashItemAsync()
	{
		if (SelectedItem is { HasHash: true, HasSimilarity: false }) {
			var ok = SelectedItem.TryCalculateSimilarity(Query.AllocImage);
			this.RaisePropertyChanged(nameof(SelectedItem.Similarity));
		}
	}


	// [RelayCommand]
	public async Task UploadInputAsync()
	{
		Query   = await SearchQuery.TryCreateAsync(Input.Trim('\"'));
		IsReady = await Query.TryUploadAsync();

		if (IsReady) {
			Trace.Assert(Query.Upload != null);

			// IsReady = Query.IsUploaded;
			Url   = Query.Upload.Url;
			Image = new Bitmap(Query.AllocImage.GetSource());
		}
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
		Query?.Dispose();
	}

	private async void ClipboardTick(object? sender, EventArgs args)
	{
		if (Application.Current == null || IsReady) {
			return;
		}

		var tl        = Application.Current.GetTopLevel();
		var clipboard = tl?.Clipboard;
		
		if (clipboard == null)
			return;

		// var formats = await clipboard.GetDataFormatsAsync();
		// var data    = await clipboard.TryGetDataAsync();
		// foreach (var fmt in formats) { }

		var clipFile = await clipboard.TryGetFileAsync();

		if (Path.Exists(clipFile?.Path.LocalPath)) {
			Input = clipFile.TryGetLocalPath();
			m_cbDispatch.Stop();
			
		}
	}

}