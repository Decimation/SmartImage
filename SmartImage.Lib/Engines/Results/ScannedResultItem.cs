// Author: Deci | Project: SmartImage.Lib | Name: ScannedResultItem.cs
// Date: 2026/02/26 @ 22:02:21

using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using SmartImage.Lib.Images;
using SmartImage.Lib.Images.Alloc;

// ReSharper disable UnusedVariable

// ReSharper disable UnassignedGetOnlyAutoProperty

namespace SmartImage.Lib.Engines.Results;

public class ScannedResultItem : IAllocImageView<AllocImage>, IChildResultItem, IDisposable, IAllocSourceItem<ScannedResultItem>
{

	public SearchResult Root { get; }

	public IResultItem Parent { get; }

	[MNNW(true, nameof(Parent))]
	public bool IsChild { get; }

	public AllocImage AllocImage { get; }

	internal ScannedResultItem(IResultItem parent)
	{
		Parent     = parent;
		Root       = parent.Root;
		IsChild    = true;
		AllocImage = new AllocImageUrl(parent.Url);
	}


	public static async Task<ScannedResultItem> FromSourceAsync(object            src, IResultItem item, bool autoInit = true, bool autoDisposeOnError = true,
	                                                            CancellationToken ct = default)
	{
		var url = (Url) src;
		var sri = new ScannedResultItem(item);
		var (allocOk, allocImgOk) = await sri.AllocImage.AllocAllAsync(ct);

		if (allocOk) {
			return sri;
		}
		else if (autoDisposeOnError) {
			sri?.Dispose();
		}

		return sri;
	}

	public void Dispose()
	{
		AllocImage?.Dispose();
	}

}