using System.Dynamic;
using Flurl;
using Kantan.Model;
using Novus.FileTypes;

namespace SmartImage.Lib;

public record SearchResultItem
{
	[NN]
	public SearchResult Root { get; }

	[MN]
	public Url Url { get; internal set; }

	[CBN]
	public string Title { get; internal set; }

	[CBN]
	public string Source { get; internal set; }

	public double? Width { get; internal set; }

	public double? Height { get; internal set; }

	[CBN]
	public string Artist { get; internal set; }

	[CBN]
	public string Description { get; internal set; }

	[CBN]
	public string Character { get; internal set; }

	[CBN]
	public string Site { get; internal set; }

	public double? Similarity { get; internal set; }

	public dynamic Metadata { get; internal set; }

	internal SearchResultItem(SearchResult r)
	{
		Root     = r;
		Metadata = new ExpandoObject();
	}

	#region Overrides of Object

	public override string ToString()
	{
		return $"[link]{Url}[/] {Similarity / 100:P} {Artist} {Description} {Site} {Source} {Title} {Character}";
	}

	#endregion

	public static bool Validate([CBN] SearchResultItem r)
	{
		return r switch
		{
			not { } => false,
			_       => true
		};
	}
}