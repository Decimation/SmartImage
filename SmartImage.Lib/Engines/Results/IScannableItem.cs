// Author: Deci | Project: SmartImage.Lib | Name: IScannableItem.cs
// Date: 2026/06/28 @ 14:06:34

namespace SmartImage.Lib.Engines.Results;

public interface IScannableItem<TItem> where TItem : IResultItem
{

	List<IResultItem> ScannedItems { get; }

	bool HasScannedItems { get; }

	ValueTask<bool> ScanAsync(CancellationToken ct = default);

}