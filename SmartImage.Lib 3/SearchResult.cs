using Flurl;
using SmartImage.Lib.Engines;

namespace SmartImage.Lib;

public enum SearchResultStatus
{
	None,
	Success,
	Cooldown,
	NoResults,
	Failure,
	Unavailable,
	Extraneous
}

/// <summary>
/// Root search result returned by a <see cref="BaseSearchEngine"/>
/// </summary>
public sealed class SearchResult : IDisposable
{
	/// <summary>
	/// Engine
	/// </summary>
	public BaseSearchEngine Engine { get; }

	/// <summary>
	/// Undifferentiated result URL
	/// </summary>
	public Url RawUrl { get; internal set; }

	public List<SearchResultItem> Results { get; internal set; }

	[CBN]
	public string ErrorMessage { get; internal set; }

	public SearchResultStatus Status { get; internal set; }

	[CBN]
	public string Overview { get; internal set; }

	[CBN]
	public SearchResultItem First
	{
		get { return Results.FirstOrDefault(r => Url.IsValid(r.Url)); }
	}

	public SearchResultItem Best
	{
		get { return Results.MaxBy(r => r.Similarity ?? 0); }
	}

	internal SearchResult(BaseSearchEngine bse)
	{
		Engine  = bse;
		Results = new List<SearchResultItem>();
	}

	public override string ToString()
	{
		return $"[{Engine.Name}] {RawUrl} | {Results.Count} | {Status} {ErrorMessage}";
	}

	#region IDisposable

	public void Dispose()
	{
		foreach (SearchResultItem item in Results) {
			item.Dispose();
		}
	}

	#endregion

	public void Update()
	{
		bool any = Results.Any();

		if (!any) {
			Status = SearchResultStatus.NoResults;
		}
		else {
			Status = SearchResultStatus.Success;

			foreach (var v in Results) {
				v.UpdateScore();
			}
		}

	}
}