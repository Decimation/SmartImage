// Author: Deci | Project: SmartImage.Lib | Name: IResultConverter.cs
// Date: 2025/04/16 @ 01:04:44

using AngleSharp.Dom;

namespace SmartImage.Lib.Results.Data;

public interface IResultConverter<out TResult> /*where TResult : IResultConvertable*/
{

	public TResult Parse(INode n, SearchResult sr);

}

public interface IResultConverter2<out TResult> /*where TResult : IResultConvertable*/
{

	public IEnumerable<SearchResultItem> ToResultItem(SearchResult sr);

	public static abstract TResult Parse(INode n);

}