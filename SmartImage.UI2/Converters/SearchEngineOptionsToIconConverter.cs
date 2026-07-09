using System;
using System.Globalization;
using System.IO;
using System.Linq.Expressions;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Search.Base;

namespace SmartImage.UI2.Converters;

internal class SearchEngineOptionsToIconConverter : IValueConverter
{

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		Stream? assetStream = null;

		if (value is SearchEngineOptions options) {
			// Map each SearchEngineOptions value to an icon path
			var icoName = options switch
			{
				SearchEngineOptions.SauceNao     => "SauceNao.ico",
				SearchEngineOptions.ImgOps       => "ImgOps.ico",
				SearchEngineOptions.GoogleImages => "GoogleImages.ico",
				SearchEngineOptions.TinEye       => "TinEye.ico",
				SearchEngineOptions.Iqdb         => "Iqdb.ico",
				SearchEngineOptions.Iqdb3D       => "Iqdb3D.ico",
				SearchEngineOptions.TraceMoe     => "TraceMoe.png",
				SearchEngineOptions.Yandex       => "Yandex.ico",
				SearchEngineOptions.Bing         => "Bing.ico",
				SearchEngineOptions.Ascii2D      => "Ascii2D.ico",
				SearchEngineOptions.EHentai      => "EHentai.png",
				SearchEngineOptions.ArchiveMoe   => "ArchivedMoe.ico",
				SearchEngineOptions.Fluffle      => "Fluffle.ico",
				SearchEngineOptions.GoogleLens   => "GoogleImages.ico",

				_ => "help.png"
			};
			var icoUri = new Uri($"avares://SmartImage.UI2/Assets/Engines/{icoName}");
			assetStream = AssetLoader.Open(icoUri);
			var img = new Bitmap(assetStream);
			return img;
		}

		throw new InvalidOperationException();
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}

}