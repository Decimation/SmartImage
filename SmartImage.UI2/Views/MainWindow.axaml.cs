using Avalonia.Controls;
using Avalonia.Input;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Results;
using SmartImage.UI2.ViewModels;

namespace SmartImage.UI2.Views;

public partial class MainWindow : Window
{

	public MainWindow()
	{
		InitializeComponent();
	}


	private void Lb_Items_OnDoubleTapped(object? sender, TappedEventArgs e)
	{
		if (sender is ListBox lb && this.DataContext is MainWindowViewModel vm) {
			var selectedItem = lb.SelectedItem as SearchResultItem;
			SearchClient.OpenResult(selectedItem.Url);
		}
	}

}