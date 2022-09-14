using Flurl;
using SmartImage.Lib.Engines;

namespace SmartImage.Lib;

public enum SearchResultStatus
{
	None,
	Cooldown,
	NoResults,
	Failure,
	Unavailable,
	Extraneous
}

public sealed class SearchResult
{
	public BaseSearchEngine Engine { get; }

	public Url RawUrl { get; internal set; }

	public List<SearchResultItem> Results { get; internal set; }

	public string ErrorMessage { get; internal set; }

	public SearchResultStatus Status { get; internal set; }

	public string Overview { get; internal set; }

	[CBN]
	public SearchResultItem First
	{
		get { return Results.FirstOrDefault(r => r.Url is { }); }
	}

	internal SearchResult(BaseSearchEngine bse)
	{
		Engine  = bse;
		Results = new List<SearchResultItem>();
	}

	public override string ToString()
	{
		return $"[{Engine.Name}] {RawUrl} | {Results.Count} | {Status}";
	}
}