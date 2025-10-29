// Author: Deci | Project: SmartImage.UI2 | Name: AppExtensions.cs
// Date: 2025/10/28 @ 23:10:57

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.VisualTree;

namespace SmartImage.UI2.Views;

public static class AppExtensions
{

	public static TopLevel? GetTopLevel(this Application app)
	{
		if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
			return desktop.MainWindow;
		}

		if (app.ApplicationLifetime is ISingleViewApplicationLifetime viewApp) {
			var visualRoot = viewApp.MainView?.GetVisualRoot();
			return visualRoot as TopLevel;
		}

		return null;
	}

}