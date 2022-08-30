using Flurl;

namespace SmartImage_3.Lib;

public enum SearchResultStatus
{
	None,
	Cooldown,
	NoResults
}

public class SearchResult
{
	public Url RawUrl { get; internal set; }

	public List<SearchResultItem> Results { get; internal set; }

	public string ErrorMessage { get; internal set; }

	public SearchResultStatus Status { get; internal set; }

	internal SearchResult()
	{
		Results = new List<SearchResultItem>();

	}

	public override string ToString()
	{
		return $"Raw: {RawUrl} | N results: {Results.Count}";
	}
}