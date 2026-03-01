// Author: Deci | Project: SmartImage.Lib | Name: ILoadFromResult.cs
// Date: 2026/02/28 @ 11:02:37

using SmartImage.Lib.Images.Uni;

namespace SmartImage.Lib.Engines.Results;

public interface ILoadFromResult<T> where T: IUniImage, ILoadFromResult<T>
{

	public static abstract Task<T> FromResult(Url u, IResultItem item, CancellationToken ct = default);

}