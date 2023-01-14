﻿using SmartImage.Lib.Engines;

namespace SmartImage.Lib.Results;

public enum SearchResultStatus
{
	/// <summary>
	/// N/A
	/// </summary>
	None,
	/// <summary>
	/// Result obtained successfully
	/// </summary>
	Success,
	/// <summary>
	/// Engine is on cooldown due to too many requests
	/// </summary>
	Cooldown,
	/// <summary>
	/// Engine returned no results
	/// </summary>
	NoResults,
	/// <summary>
	/// Obtaining results failed due to an engine error
	/// </summary>
	Failure,

	IllegalInput,
	/// <summary>
	/// Engine is unavailable
	/// </summary>
	Unavailable,
	/// <summary>
	/// Result is extraneous
	/// </summary>
	Extraneous,

}

/// <summary>
/// Root search result returned by a <see cref="BaseSearchEngine"/>
/// </summary>
public sealed class SearchResult : IDisposable
{
	/// <summary>
	/// Engine which returned this result
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

	[MN]
	public SearchResultItem Best
	{
		get
		{
			if (!Results.Any())
			{
				return null;
			}

			return Results.MaxBy(r => r.Score);
		}
	}

	internal SearchResult(BaseSearchEngine bse)
	{
		Engine = bse;
		Results = new List<SearchResultItem>();
	}

	public override string ToString()
	{
		return $"[{Engine.Name}] {RawUrl} | {Results.Count} | {Status} {ErrorMessage}";
	}

	public void Dispose()
	{
		foreach (SearchResultItem item in Results)
		{
			item.Dispose();
		}
	}

	public void Update()
	{
		bool any = Results.Any();

		/*if (!any && Status != SearchResultStatus.None) {
			Status = SearchResultStatus.NoResults;
		}
		else {
			Status = SearchResultStatus.Success;
		}*/

		foreach (var v in Results)
		{
			v.UpdateScore();
		}

	}
}