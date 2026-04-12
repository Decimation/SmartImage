// Author: Deci | Project: SmartImage.Lib | Name: ScannedResultItem.cs
// Date: 2026/02/26 @ 22:02:21

using Microsoft.Extensions.Logging;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Model;
using System.Threading.Channels;
using SmartImage.Lib.Images;
// ReSharper disable UnusedVariable

// ReSharper disable UnassignedGetOnlyAutoProperty

namespace SmartImage.Lib.Engines.Results;

public class ScannedResultItem : UniImageUrl, IResultItem, ILoadFromResult<ScannedResultItem>
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


	public static Task<ScannedResultItem> FromResult(IResultItem item, CancellationToken ct = default) => FromResult(item.Url, item, ct);

	public static async Task<ScannedResultItem> FromResult(Url u, IResultItem item, CancellationToken ct = default)
	{
		var sri = new ScannedResultItem(u, item);
		var (allocOk, allocImgOk) = await sri.AllocAllAsync(ct);

		if (allocImgOk) {
			return sri;
		}
		else {
			sri?.Dispose();
		}

		return sri;
	}

}