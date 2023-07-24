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

public class ResultItem : IDisposable
{
	public string Name { get; }

	public SearchResultItem Result { get; }

	public SearchResultStatus Status { get; }

	public BitmapImage StatusImage { get; internal set; }

	// public Url? Url => Uni != null ? Uni.Value.ToString() : Result.Url;
	public Url? Url { get; protected set; }

	public ResultItem(SearchResultItem result, string name)
	{
		Result = result;
		Name   = name;
		Status = result.Root.Status;
		Url    = result.Url;

		if (Status.IsSuccessful()) {
			StatusImage = AppComponents.accept;
		}
		else if (Status.IsUnknown()) {
			StatusImage = AppComponents.help;
		}
		else /*if (Status.IsError())*/ {
			StatusImage = AppComponents.exclamation;
		}

	}

	public void Dispose()
	{
		Result.Dispose();
	}
}

public class UniResultItem : ResultItem
{
	public UniResultItem(ResultItem ri, int? idx) 
		: base(ri.Result, $"{ri.Name} ({idx})")
	{
		UniIndex    = idx;
		Url         = Uni?.Value.ToString();
		StatusImage = AppComponents.picture;
	}

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
}