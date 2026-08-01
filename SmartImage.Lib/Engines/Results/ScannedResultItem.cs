// Author: Deci | Project: SmartImage.Lib | Name: ScannedResultItem.cs
// Date: 2026/02/26 @ 22:02:21

using System.ComponentModel;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using SmartImage.Lib.Images;
using SmartImage.Lib.Images.Alloc;
using SmartImage.Lib.Utilities;

// ReSharper disable UnusedVariable

// ReSharper disable UnassignedGetOnlyAutoProperty

namespace SmartImage.Lib.Engines.Results;

public class ScannedResultItem : IChildResultItem, IAllocImageView<IAllocImage>, IAllocSourceItem<ScannedResultItem, IResultItem>, IDisposable, IResultItem
{

	private static readonly ILogger s_logger = AppSupport.Factory.CreateLogger(nameof(ScannedResultItem));

	public SearchResult Root { get; }

	public bool IsRaw { get; }

	public IResultItem Parent { get; }

	[MNNW(true, nameof(Parent))]
	public bool IsChild { get; }

	public IAllocImage AllocImage { get; }

	public int SubIndex => ((IScannableItem) Parent).ScannedItems.IndexOf(this);

	internal ScannedResultItem(IResultItem parent, IAllocImage allocImg = null)
	{
		Parent     = parent;
		Root       = parent.Root;
		IsChild    = true;
		AllocImage = allocImg ?? new AllocImageUrl(parent.Url);
	}

	public int? Width
	{
		get => AllocImage?.Width ?? Parent.Width;
		set { }
	}

	public int? Height
	{
		get => AllocImage?.Height ?? Parent.Height;
		set { }
	}

	public string Title
	{
		get => field ?? Parent.Title;
		internal set;
	}

	public string Source
	{
		get => field ?? Parent.Source;
		internal set;
	}

	public string Artist
	{
		get => field ?? Parent.Artist;
		internal set;
	}

	public string Description
	{
		get => field ?? Parent.Description;
		internal set;
	}

	public string Character
	{
		get => field ?? Parent.Character;
		internal set;
	}

	public string Site
	{
		get => field ?? Parent.Site;
		internal set;
	}

	public DateTime? Time
	{
		get => field ?? Parent.Time;
		internal set;
	}

	public double? Similarity
	{
		get => field ?? Parent.Similarity;
		set;
	}

	public ulong? Hash
	{
		get => field ?? Parent.Hash;
		set;
	}

	public Url Url => ((AllocImageUrl) AllocImage).Url;

	public event PropertyChangedEventHandler PropertyChanged;

	public void Dispose()
	{
		AllocImage?.Dispose();
	}

}