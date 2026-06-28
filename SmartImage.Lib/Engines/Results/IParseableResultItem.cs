// Author: Deci | Project: SmartImage.Lib | Name: IParseableResultItem.cs
// Date: 2025/04/16 @ 01:04:44

namespace SmartImage.Lib.Engines.Results;

public interface IParseableResultItem<in TSource, out TItem> : IParseableSource<TSource, SearchResult, TItem> where TItem : IResultItem { }