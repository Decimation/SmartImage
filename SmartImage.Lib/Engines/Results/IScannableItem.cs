// Author: Deci | Project: SmartImage.Lib | Name: IScannableItem.cs
// Date: 2026/06/28 @ 14:06:34

using SmartImage.Lib.Images.Alloc;
using SmartImage.Lib.Model;
using System.Threading.Channels;
using SmartImage.Lib.Images;

namespace SmartImage.Lib.Engines.Results;

public interface IScannableItem : IResultItem
{

	List<ScannedResultItem> ScannedItems { get; }

	bool HasScannedItems { get; }


	ValueTask<bool> ScanAsync(CancellationToken ct = default);

	

}