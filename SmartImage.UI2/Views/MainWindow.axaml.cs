using System;
using AsyncImageLoader;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Threading;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Model;
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
			var selectedItem = lb.SelectedItem as IUrl;
			SearchClient.OpenResult(selectedItem.Url);
		}
	}

	private void Tb_Input_OnTextChanging(object? sender, TextChangingEventArgs e)
	{
		if (sender is TextBox lb && this.DataContext is MainWindowViewModel vm) {
			var selectedItem = lb.Text;
		}

	}

}