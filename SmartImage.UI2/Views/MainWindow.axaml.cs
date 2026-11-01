using System;
using System.ComponentModel;
using AsyncImageLoader;
using AsyncImageLoader.Core.Caching;
using AsyncImageLoader.Core.Pipeline;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Threading;
using Kantan.Utilities;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Primitives.Disposables;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Engines.Search.Base;
using SmartImage.Lib.Model;
using SmartImage.UI2.Converters;
using SmartImage.UI2.ViewModels;

namespace SmartImage.UI2.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{

	public MainWindow()
	{
		InitializeComponent();
		


		Opened += MainWindow_OnOpened;

	}

	private void MainWindow_OnOpened(object? sender, EventArgs e) { }


	private void Lb_Items_OnDoubleTapped(object? sender, TappedEventArgs e)
	{

		if (sender is ListBox lb && this.DataContext is MainWindowViewModel vm) {
			var selectedItem = vm.SelectedItem;
			SearchClient.OpenResult(selectedItem.Url);
		}
	}

}