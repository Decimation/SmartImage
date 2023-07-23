// Read S SmartImage.UI ResultItem.cs
// 2023-07-17 @ 5:54 PM

using System;
using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using Flurl;
using Novus.FileTypes;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;

namespace SmartImage.UI;

public sealed class ListResultItem : IDisposable
{
	public string Name { get; }

	public SearchResultItem Result { get; }

	public SearchResultStatus Status { get; }

	public BitmapImage Image { get; }

	public UniSource? Uni
	{
		get
		{
			if (UniIndex.HasValue && Result.Uni != null) {
				return Result.Uni[UniIndex.Value];

			}

			return null;
		}
	}

	public int? UniIndex { get; }

	public Url? Url => Uni != null ? Uni.Value.ToString() : Result.Url;

	public ListResultItem(SearchResultItem result, string name, SearchResultStatus status, int? idx = default)
	{
		Result   = result;
		Name     = name;
		Status   = status;
		UniIndex = idx;

		if (Status.IsSuccessful()) {
			Image = AppComponents.accept;
		}
		else if (Status.IsUnknown()) {
			Image = AppComponents.help;
		}
		else {
			Image = AppComponents.exclamation;
		}
		
	}

	public void Dispose()
	{
		Result.Dispose();
	}
}