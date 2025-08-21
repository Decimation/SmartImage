// Author: Deci | Project: SmartImage.Lib | Name: IParseable.cs
// Date: 2025/08/07 @ 03:08:18

namespace SmartImage.Lib.Model;

public interface IParseable<in TSource, out TItem>
{

	public static abstract TItem Parse(TSource n);

}