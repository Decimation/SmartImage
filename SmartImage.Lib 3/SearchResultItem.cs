using Flurl;

namespace SmartImage_3.Lib;

public class SearchResultItem
{
	public Url Url { get; internal set; }

	public string Title { get; internal set; }

	public string Source { get; internal set; }

	public string Artist { get; internal set; }

	public string Description { get; internal set; }

	public double Similarity { get; internal set; }

	public SearchResult Root { get; }

	internal SearchResultItem(SearchResult r)
	{
		Root = r;

	}

	#region Overrides of Object

	public override string ToString()
	{
		return $"{Root} :: {Url} | {Similarity} {Artist} {Source}";
	}

	#endregion
}