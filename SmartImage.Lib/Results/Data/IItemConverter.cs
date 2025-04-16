// Author: Deci | Project: SmartImage.Lib | Name: IResultConverter.cs
// Date: 2025/04/16 @ 01:04:44

using AngleSharp.Dom;

namespace SmartImage.Lib.Results.Data;

public interface IItemConverter<out TResult, out TResult2>
	: IItemConvertable<TResult2>
/*where TResult : IResultConvertable*/
{

	// public TResult2 ToResultItem(SearchResult sr);

	public static abstract TResult Parse(INode n);

}

public interface ISearchResultItemConverter<out TResult>
	: IItemConverter<TResult, SearchResultItem> { }