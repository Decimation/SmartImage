// Author: Deci | Project: SmartImage.Lib | Name: IParseableResult.cs
// Date: 2025/04/16 @ 01:04:44

namespace SmartImage.Lib.Engines.Results;

public interface IParseableResult<in TSource, out TItem> where TItem : IResultItem
{

	public static abstract TItem ParseSource(TSource n, SearchResult r);

}