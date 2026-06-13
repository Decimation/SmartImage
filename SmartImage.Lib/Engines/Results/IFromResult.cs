// Author: Deci | Project: SmartImage.Lib | Name: IFromResult.cs
// Date: 2026/02/28 @ 11:02:37

using SmartImage.Lib.Images.Uni;

namespace SmartImage.Lib.Engines.Results;

public interface IFromResult<T> where T: IUniImage, IFromResult<T>
{

	public static abstract Task<T> AllocFromResult(Url u, IResultItem item, CancellationToken ct = default);

}