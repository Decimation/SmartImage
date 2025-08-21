// Author: Deci | Project: SmartImage.Lib | Name: IResultConverter.cs
// Date: 2025/04/16 @ 01:04:44

using SmartImage.Lib.Engines.Results;

namespace SmartImage.Lib.Model;

public interface ISearchResultItemParseable<in TSource, out TItem>
	where TItem : SearchResultItem
{

	public static abstract TItem ParseResultItem(TSource n, SearchResult r);

}