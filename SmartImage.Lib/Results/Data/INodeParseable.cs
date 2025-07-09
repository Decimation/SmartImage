// Author: Deci | Project: SmartImage.Lib | Name: IResultConverter.cs
// Date: 2025/04/16 @ 01:04:44

using AngleSharp.Dom;

namespace SmartImage.Lib.Results.Data;

/*public interface ISearchResultItemParseable<out TItem> where TItem : SearchResultItem
/*where TResult : IResultConvertable#1#
{

	// public TResult2 ToResultItem(SearchResult sr);

	public static abstract TItem Parse(SearchResult sr, INode n);

}*/

public interface IResultParse<in TSource, TSearchResultItem>
	where TSearchResultItem : SearchResultItem
{

	public static abstract ValueTask<TSearchResultItem> ParseResultItem(TSource n, SearchResult r);

}

public interface INodeResultParse<TSearchResultItem> : IResultParse<INode, TSearchResultItem>
	where TSearchResultItem : SearchResultItem { }


public interface INodeParseable<out TResult> : IToSearchResultItem
{

	public static abstract TResult Parse(INode n);

}

public interface INodeToSearchResultItemParseable<out TResult>
	: INodeParseable<TResult> { }