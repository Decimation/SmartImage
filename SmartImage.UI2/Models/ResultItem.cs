// Author: Deci | Project: SmartImage.UI2 | Name: Models.cs
// Date: 2025/08/06 @ 13:08:44

using System.Collections.ObjectModel;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Images;

namespace SmartImage.UI2.Models;

public class ResultItem : ObservableObject
{
	public SearchResultItem Item {get;}

	public ResultItem(SearchResultItem sri)
	{
		Item = sri;

		// var stream = sri.Uni[0].Image.ToStream();
		// Image = WriteableBitmap.Decode(stream);
	}

}