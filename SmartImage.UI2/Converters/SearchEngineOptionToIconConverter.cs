using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq.Expressions;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Engines.Search.Base;

namespace SmartImage.UI2.Converters;
// todo: cache these assets instead of converting and loading at runtime
internal class SearchEngineOptionToIconConverter : IValueConverter
{

	internal static readonly Dictionary<SearchEngineOptions, string> EngineIcons = new()
	{
		{ SearchEngineOptions.SauceNao, "SauceNao.ico" },
		{ SearchEngineOptions.ImgOps, "ImgOps.ico" },
		{ SearchEngineOptions.GoogleImages, "GoogleImages.ico" },
		{ SearchEngineOptions.TinEye, "TinEye.ico" },
		{ SearchEngineOptions.Iqdb, "Iqdb.ico" },
		{ SearchEngineOptions.Iqdb3D, "Iqdb3D.ico" },
		{ SearchEngineOptions.TraceMoe, "TraceMoe.png" },
		{ SearchEngineOptions.Yandex, "Yandex.ico" },
		{ SearchEngineOptions.Bing, "Bing.ico" },
		{ SearchEngineOptions.Ascii2D, "Ascii2D.ico" },
		{ SearchEngineOptions.EHentai, "EHentai.png" },
		{ SearchEngineOptions.ArchiveMoe, "ArchivedMoe.ico" },
		{ SearchEngineOptions.Fluffle, "Fluffle.ico" },
		{ SearchEngineOptions.GoogleLens, "GoogleImages.ico" }

	};

	private const string URI_ASSETS = "avares://SmartImage.UI2/Assets";

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		Stream? assetStream = null;

		Uri icoUri;

		if (value is SearchEngineOptions seo && EngineIcons.TryGetValue(seo, out var icoName)) {
			icoUri = new Uri(Path.Combine(URI_ASSETS, $"Engines", icoName));
		}
		else {
			icoUri = new Uri(Path.Combine(URI_ASSETS, "help.png"));
		}

		assetStream = AssetLoader.Open(icoUri);
		var img = new Bitmap(assetStream);
		return img;

		throw new InvalidOperationException();
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}

}