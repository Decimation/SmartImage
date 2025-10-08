// Author: Deci | Project: SmartImage.UI2 | Name: MainWindowViewModel.cs
// Date: 2025/08/06 @ 11:08:36

using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader;
using AsyncImageLoader.Loaders;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Results;
using SmartImage.UI2.Models;

namespace SmartImage.UI2.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{

	public ObservableCollection<SearchResultItem> Items { get; } = new();

	public SearchClient Client { get; }

	public SearchConfig Config { get; }

	public SearchQuery Query { get; set; }

	public string Input { get; set; }


	[ObservableProperty]
	public partial IImage Image { get; set; }

	public CancellationTokenSource TokenSource { get; }

	public MainWindowViewModel()
	{
		Config      = new SearchConfig();
		Client      = new SearchClient(Config);
		TokenSource = new CancellationTokenSource();
	}

	[ObservableProperty]
	public partial SearchResultItem SelectedItem { get; set; }

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
		var ok = SelectedItem.CalculateSimilarity(Query.Source);

	}
	[ObservableProperty]
	public partial double Progress {get;set;}

	[RelayCommand]
	public async Task SearchAsync()
	{
		Query = await SearchQuery.TryCreateAsync(Input?.Trim('\"'));
		await Query.TryUploadAsync();
		Image = new Bitmap(Query.Source.GetStream());
		var r = Client.RunSearchAsync(Query);

		var max = Client.Engines.Count();
		int c = 0;
		while (await Client.ResultChannel.Reader.WaitToReadAsync()) {
			var res = await Client.ResultChannel.Reader.ReadAsync();
			
			foreach (var item in res.Results) {
				Items.Add((item));
			}
			Progress = (++c / (double) max)*100d;
		}

		await r;

	}

}