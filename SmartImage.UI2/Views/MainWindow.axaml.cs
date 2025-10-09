using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Results;
using SmartImage.UI2.ViewModels;

namespace SmartImage.UI2.Views;
static class AppExtensions
{

	public static TopLevel? GetTopLevel(this Application app)
	{
		if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			return desktop.MainWindow;
		}
		if (app.ApplicationLifetime is ISingleViewApplicationLifetime viewApp)
		{
			var visualRoot = viewApp.MainView?.GetVisualRoot();
			return visualRoot as TopLevel;
		}
		return null;
	}

}
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

	private void Tb_Input_OnTextChanging(object? sender, TextChangingEventArgs e)
	{
		if (sender is TextBox lb && this.DataContext is MainWindowViewModel vm) {
			var selectedItem = lb.Text;
		}

	}

}