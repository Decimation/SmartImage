// Author: Deci | Project: SmartImage.UI2 | Name: MainWindowViewModel.cs
// Date: 2025/08/06 @ 11:08:36

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using SmartImage.Lib.Engines.Results;
using SmartImage.UI2.Models;

namespace SmartImage.UI2.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{

	public MainWindowViewModel() { }

	public ObservableCollection<ResultItem> Items { get; } = new()
	{
	};

	[RelayCommand]
	public async Task SearchAsync()
	{

	}

}