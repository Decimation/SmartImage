// Author: Deci | Project: SmartImage.Lib | Name: IParseableSource.cs
// Date: 2026/05/24 @ 01:05:37

namespace SmartImage.Lib.Engines.Results;

public interface IParseableSource<in TSource, in TData, out TItem>
{

	public static abstract TItem ParseSource(TSource n, TData data);

}