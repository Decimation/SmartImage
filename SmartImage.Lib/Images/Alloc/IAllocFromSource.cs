// Author: Deci | Project: SmartImage.Lib | Name: IAllocFromSource.cs
// Date: 2026/02/28 @ 01:02:42

using SmartImage.Lib.Engines.Results;

namespace SmartImage.Lib.Images.Alloc;

public interface IAllocFromSource<TItem> where TItem : IAllocImage
{

	static abstract Task<TItem> FromSourceAsync(object src, bool autoInit = true, bool autoDisposeOnError = true, CancellationToken ct = default);


}

public interface IAllocSourceItem<TItem, in TResultItem> /*where TItem:IAllocImage*/ where TResultItem : IResultItem { }