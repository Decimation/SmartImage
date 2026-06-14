// Author: Deci | Project: SmartImage.Lib | Name: IParseableItem.cs
// Date: 2026/05/24 @ 01:05:37

namespace SmartImage.Lib.Engines.Results;

public interface IParseableItem<in TSource, in TData, out TItem>
{

	public static abstract TItem ParseSource(TSource n, TData data);

}