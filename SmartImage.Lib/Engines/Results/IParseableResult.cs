// Author: Deci | Project: SmartImage.Lib | Name: IParseableResult.cs
// Date: 2025/04/16 @ 01:04:44

using SmartImage.Lib.Model;

namespace SmartImage.Lib.Engines.Results;

public interface IParseableResult<in TSource, out TItem> : IParseableItem<TSource, SearchResult, TItem> where TItem : IResultItem { }