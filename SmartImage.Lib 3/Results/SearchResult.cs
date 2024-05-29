using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using Flurl.Http;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Results;

// [Flags]
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
public sealed class SearchResult : IDisposable, INotifyPropertyChanged
{

	/// <summary>
	/// Engine which returned this result
	/// </summary>
	public BaseSearchEngine Engine { get; }

	/// <summary>
	/// Undifferentiated result URL
	/// </summary>
	public Url RawUrl { get; internal set; }

	/// <summary>
	/// Results; first element should be <see cref="GetRawResultItem"/>
	/// </summary>
	public List<SearchResultItem> Results { get; }

	[CBN]
	public string ErrorMessage { get; internal set; }

	public SearchResultStatus Status { get; internal set; }

	[CBN]
	public string Overview { get; internal set; }

	[CBN]
	public SearchResultItem GetBestResult()
	{
		if (Results.Count == 0) {
			// This should never happen so long as results contains the raw item
			Debugger.Break();
			return null;
		}

		return Results.OrderByDescending(r => r.Similarity)
			.FirstOrDefault(r => Url.IsValid(r.Url));
	}

	internal SearchResult(BaseSearchEngine bse)
	{
		Engine  = bse;
		Results = [];

		// Results = [GetRawResultItem()];
	}

	public override string ToString()
	{
		return $"[{Engine.Name}] {RawUrl} | {Results.Count} | {Status} {ErrorMessage}";
	}

	public void Dispose()
	{
		foreach (SearchResultItem item in Results) {
			item.Dispose();
		}
	}

	public void Update()
	{
		if (Status.IsError()) {
			return;
		}

		bool any = Results.Count != 0;

		if (!any) {
			Status = SearchResultStatus.NoResults;
		}
		else {
			Status = SearchResultStatus.Success;
		}
		/*if (!any && Status != SearchResultStatus.None) {
			Status = SearchResultStatus.NoResults;
		}
		else {
			Status = SearchResultStatus.Success;
		}*/

		foreach (var v in Results) {
			v.UpdateScore();
		}

	}

	public event PropertyChangedEventHandler PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value)) return false;

		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}

	public SearchResultItem GetRawResultItem()
	{
		return new SearchResultItem(this)
		{
			IsRaw = true,
			Url   = RawUrl
		};
	}

}