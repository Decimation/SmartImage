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

namespace SmartImage.UI;

public sealed class ResultItem : IDisposable
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

	public ResultItem(SearchResultItem result, string name, SearchResultStatus status, int? idx = default)
	{
		Result   = result;
		Name     = name;
		Status   = status;
		UniIndex = idx;

		Image = Status switch
		{
			SearchResultStatus.None or SearchResultStatus.Success => AppControls.accept,
			SearchResultStatus.Failure                            => AppControls.exclamation,
			_                                                     => AppControls.help
		};
	}

	public void Dispose()
	{
		Result.Dispose();
	}
}