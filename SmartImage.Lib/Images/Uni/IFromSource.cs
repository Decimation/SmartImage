// Author: Deci | Project: SmartImage.Lib | Name: IFromSource.cs
// Date: 2026/02/28 @ 01:02:42

using SmartImage.Lib.Engines.Results;

namespace SmartImage.Lib.Images.Uni;

public interface IFromSource<TUniImage> where TUniImage : IUniImage
{

	static abstract Task<TUniImage> FromSourceAsync(object src, bool autoInit = true, bool autoDisposeOnError = true, CancellationToken ct = default);

}

public interface IFromSourceItem<TUniImage> where TUniImage : IUniImage
{

	static abstract Task<TUniImage> FromSourceAsync(object src, IResultItem item, bool autoInit = true, bool autoDisposeOnError = true, CancellationToken ct = default);

}