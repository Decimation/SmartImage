// Author: Deci | Project: SmartImage.Lib | Name: ScannedResultItem.cs
// Date: 2026/02/26 @ 22:02:21

using Microsoft.Extensions.Logging;
using SmartImage.Lib.Images.Uni;
using System.Threading.Channels;
using SmartImage.Lib.Images;

// ReSharper disable UnusedVariable

// ReSharper disable UnassignedGetOnlyAutoProperty

namespace SmartImage.Lib.Engines.Results;

public class ScannedResultItem : UniImageUrl, IResultItem, IFromSourceItem<ScannedResultItem>, IFromSource<ScannedResultItem>, ISubResultItem
{

	public SearchResult Root { get; }

	public IResultItem Parent { get; }

	[MNNW(true, nameof(Parent))]
	public bool IsChild { get; }

	public bool IsRaw => false;

	public string Title { get; }

	public string Source { get; }

	public string Artist { get; }

	public string Description { get; }

	public string Character { get; }

	public string Site { get; }

	public DateTime? Time { get; }

	public int? Width { get; }

	public int? Height { get; }

	// public int Index => Parent is SearchResultItem sri ? sri.ScannedItems.IndexOf(this) : -1;


	internal ScannedResultItem(Url url, IResultItem parent) : base(url)
	{
		Parent  = parent;
		Root    = parent.Root;
		IsChild = true;

		// todo
		Title       = parent.Title;
		Source      = parent.Source;
		Artist      = parent.Artist;
		Description = parent.Description;
		Character   = parent.Character;
		Site        = parent.Site;
		Time        = parent.Time;
		Width       = parent.Width;
		Height      = parent.Height;
	}


	public static async Task<ScannedResultItem> FromSourceAsync(object            src, IResultItem item, bool autoInit = true, bool autoDisposeOnError = true,
	                                                           CancellationToken ct = default)
	{
		var url = src as Url;
		var sri = new ScannedResultItem(url, item);
		var (allocOk, allocImgOk) = await sri.AllocAllAsync(ct);

		if (allocImgOk) {
			return sri;
		}
		else {
			sri?.Dispose();
		}

		return sri;
	}

	public static Task<ScannedResultItem> FromSourceAsync(IResultItem item, bool autoInit = true, bool autoDisposeOnError = true, CancellationToken ct = default)
		=> FromSourceAsync(item.Url, item, autoInit, autoDisposeOnError, ct);

	public new static Task<ScannedResultItem> FromSourceAsync(object src, bool autoInit = true, bool autoDisposeOnError = true, CancellationToken ct = default)
	{
		if (src is IResultItem ri) {
			return FromSourceAsync(ri, autoInit, autoDisposeOnError, ct);
		}

		throw new ArgumentException(null, nameof(src));
	}

}