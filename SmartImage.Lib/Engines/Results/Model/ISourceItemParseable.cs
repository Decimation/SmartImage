// Author: Deci | Project: SmartImage.Lib | Name: IResultConverter.cs
// Date: 2025/04/16 @ 01:04:44

namespace SmartImage.Lib.Engines.Results.Model;

public interface ISearchResultItems<T> where T : SearchResultItem
{

	public static abstract T Parse(T[] t, SearchResult r);

}

public interface ISourceItemParseable<in TSource, out TItem>
	where TItem : SearchResultItem
{

	public static abstract TItem ParseResultItem(TSource n, SearchResult r);

}