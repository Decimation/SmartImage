// Read S SmartImage.UI ResultItem.cs
// 2023-07-17 @ 5:54 PM

using System;
using System.Diagnostics.CodeAnalysis;
using Flurl;
using SmartImage.Lib.Results;

namespace SmartImage.UI;

public sealed class ResultItem : IDisposable
{
	public string Name { get; }

	[MaybeNull]
	public SearchResultItem? Result { get; }

	public Url? Url { get; }

	public ResultItem(SearchResultItem? result, string name, Url? u = default)
	{
		Result = result;

		Name = name;
		Url  = u ?? result?.Url;
	}
	
	public void Dispose()
	{
		Result.Dispose();
	}
}