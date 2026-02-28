// Author: Deci | Project: SmartImage.Lib | Name: ScannedResultItem.cs
// Date: 2026/02/26 @ 22:02:21

using Microsoft.Extensions.Logging;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Model;
using System.Threading.Channels;
using SmartImage.Lib.Images;

// ReSharper disable UnassignedGetOnlyAutoProperty

namespace SmartImage.Lib.Engines.Results;

public class ScannedResultItem : UniImageUrl, IResultItem
{

	public SearchResult Root { get; }

	public IResultItem Parent { get; }

	public bool IsChild { get; }

	public bool IsRaw => false;

	public int? Width { get; }

	public int? Height { get; }

	public virtual bool CalculateSimilarity(IHashable hashable)
	{
		Similarity = ISimilarity.CalculateHashSimilarity(this, hashable);
		return Similarity.HasValue;
	}

	internal ScannedResultItem(Url url, IResultItem parent) : base(url)
	{
		Parent      = parent;
		Root        = parent.Root;
		IsChild     = true;
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


	public string Title { get; }

	public string Source { get; }

	public string Artist { get; }

	public string Description { get; }

	public string Character { get; }

	public string Site { get; }

	public DateTime? Time { get; }

	
}