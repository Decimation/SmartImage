// Read S SmartImage.UI ResultItem.cs
// 2023-07-17 @ 5:54 PM

using System;
using System.Diagnostics.CodeAnalysis;
using Flurl;
using Novus.FileTypes;
using SmartImage.Lib.Results;

namespace SmartImage.UI;

public sealed class ResultItem : IDisposable
{
	public        string Name { get; }

	public SearchResultItem Result { get; }

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

	public ResultItem(SearchResultItem result, string name, int? idx = default)
	{
		Result   = result;
		Name     = name;
		UniIndex = idx;
	}

	public void Dispose()
	{
		Result.Dispose();
	}
}