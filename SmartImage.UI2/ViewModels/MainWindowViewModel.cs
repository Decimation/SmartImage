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
using SmartImage.UI2.Models;
using SmartImage.UI2.Views;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using DynamicData.Binding;
using ReactiveUI;
using SmartImage.Lib.Images.Uni;

namespace SmartImage.UI2.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{

	public ObservableCollection<SearchResultItem> Items { get; } = new();

	public SearchClient Client { get; }

	public SearchConfig Config { get; }

	public SearchQuery Query { get; set; }

	public bool IsReady
	{
		get => field;
		set => this.RaiseAndSetIfChanged(ref field, value);
	}

	public string Input
	{
		get;
		set => this.RaiseAndSetIfChanged(ref field, value);
	}

	public IImage Image
	{
		get => field;
		set => this.RaiseAndSetIfChanged(ref field, value);
	}

	public CancellationTokenSource TokenSource { get; private set; }

	private DispatcherTimer m_dt;

	public MainWindowViewModel()
	{
		m_dt = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Default, (Callback));


		Config      = new SearchConfig();
		Client      = new SearchClient(Config);
		TokenSource = new CancellationTokenSource();

		Config.PropertyChanged += OnChangedEvent;

		var isVal = this.WhenAnyValue(x => x.Input, Selector);
		UploadCommand = ReactiveCommand.CreateFromTask(UploadInputAsync, isVal);

		var isUp = this.WhenAnyValue(x => x.IsReady);
		SearchCommand = ReactiveCommand.CreateFromTask(RunSearchAsync, isUp);

		/*var canScan = this.WhenAnyValue(x => x.SelectedItem, (SearchResultItem s) => { return s.HasScannedItems; });
		SearchCommand = ReactiveCommand.CreateFromTask(RunSearchAsync, isUp);*/

		ClearCommand = ReactiveCommand.CreateFromTask(ClearAsync);
	}

	private bool Selector(string x)
	{
		return UniImage.IsValidSourceType(x?.ToString());
	}

	private bool Selector2(ObservableCollection<SearchResultItem> x)
	{
		return x.Count > 0;
	}

	private void OnChangedEvent(object? sender, PropertyChangedEventArgs args)
	{
		switch (args.PropertyName) {
			case nameof(Config.Clipboard):
				if (Config.Clipboard) {
					m_dt.Start();

				}
				else {
					m_dt.Stop();
				}

				break;
		}
	}

	public SearchResultItem SelectedItem
	{
		get => field;
		set => this.RaiseAndSetIfChanged(ref field, value);
	}

	[RelayCommand]
	public async Task LoadItemAsync()
	{
		var ok = await SelectedItem.ScanAsync(TokenSource.Token);

		if (ok) {
			Items.AddOrInsertRange(SelectedItem.ScannedItems, Items.IndexOf(SelectedItem));
		}
	}

	[RelayCommand]
	public async Task HashItemAsync()
	{
		if (SelectedItem.HasHash) {
			var ok = SelectedItem.CalculateSimilarity(Query.Source);
			this.RaisePropertyChanged(nameof(SelectedItem.Similarity));
		}
	}

	public double Progress { get; set; }

	// [RelayCommand]
	public async Task UploadInputAsync()
	{
		Query   = await SearchQuery.TryCreateAsync(Input.Trim('\"'));
		IsReady = await Query.TryUploadAsync();

		// IsReady = Query.IsUploaded;
		Image = new Bitmap(Query.Source.GetStream());
	}

	public ReactiveCommand<Unit, Unit> UploadCommand { get; }

	public ReactiveCommand<Unit, Unit> SearchCommand { get; }

	public ReactiveCommand<Unit, Unit> ClearCommand { get; }


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

	private async void Callback(object? sender, EventArgs args)
	{
		if (Application.Current == null) {
			return;
		}

		var clipboard = Application.Current.GetTopLevel()?.Clipboard;

		if (clipboard == null)
			return;


		var clipn = await clipboard.TryGetFileAsync();


	}

}