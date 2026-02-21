// Author: Deci | Project: SmartImage.Lib | Name: ScannedResultItem.cs
// Date: 2026/02/21 @ 16:02:56

using SmartImage.Lib.Images.Uni;

namespace SmartImage.Lib.Engines.Results;

public interface IScannedResultItem : IResultItem, IUniImage
{
	List<IResultItem> ScannedItems { get; }

	[MNNW(true, nameof(ScannedItems))]
	bool HasScannedItems => ScannedItems is { Count: > 0 };
	

}