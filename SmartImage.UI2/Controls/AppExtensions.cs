// Author: Deci | Project: SmartImage.UI2 | Name: AppExtensions.cs
// Date: 2025/10/28 @ 23:10:57

using System;
using System.Threading.Tasks;
using AsyncImageLoader;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using SmartImage.Lib.Engines.Results;

namespace SmartImage.UI2.Controls;

public static class AppExtensions
{

	extension(Application app)
	{

		public TopLevel? GetTopLevel()
		{
			if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
				return TopLevel.GetTopLevel(desktop.MainWindow);
			}

			return null;
		}

	}

}