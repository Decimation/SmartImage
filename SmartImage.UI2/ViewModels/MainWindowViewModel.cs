// Author: Deci | Project: SmartImage.UI2 | Name: MainWindowViewModel.cs
// Date: 2025/08/06 @ 11:08:36

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Results;
using SmartImage.UI2.Models;

namespace SmartImage.UI2.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{

	public MainWindowViewModel()
	{
		Config = new SearchConfig();
		Client = new SearchClient(Config);

	}

	public ObservableCollection<ResultItem> Items { get; } = new()
		{ };

	public SearchClient Client { get; }

	public SearchConfig Config { get; }

	public SearchQuery Query { get; set; }

	[RelayCommand]
	public async Task SearchAsync()
	{
		Query = await SearchQuery.TryCreateAsync(@"C:\Users\Deci\Pictures\Epic anime\1654086015521.png");
		await Query.UploadAsync();

		var r = Client.RunSearchAsync(Query);

		while (await Client.ResultChannel.Reader.WaitToReadAsync()) {
			var res = await Client.ResultChannel.Reader.ReadAsync();

			foreach (var item in res.Results) {
				Items.Add(new ResultItem(item));
			}
		}

		await r;

	}

}