// Author: Deci | Project: SmartImage.UI2 | Name: Models.cs
// Date: 2025/08/06 @ 13:08:44

using System.Collections.ObjectModel;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Images;

namespace SmartImage.UI2.Models;

public class ResultItem
{

	public string Url { get; }

	public IImage Image { get; }

	public double? Similarity { get; }

	public ResultItem(SearchResultItem sri)
	{
		Url        = sri.Url;
		Similarity = sri.Similarity;

		// var stream = sri.Uni[0].Image.ToStream();
		// Image = WriteableBitmap.Decode(stream);
	}

}