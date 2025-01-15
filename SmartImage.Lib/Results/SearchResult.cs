using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using AngleSharp.Html.Parser;
using Flurl.Http;
using Kantan.Diagnostics;
using Kantan.Net.Utilities;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Images;
using SmartImage.Lib.Utilities;
using static SmartImage.Lib.Engines.BaseSearchEngine;

namespace SmartImage.Lib.Results;

#nullable disable

public enum SearchResultStatus
{

	/// <summary>
	/// N/A
	/// </summary>
	None = 0,

	/// <summary>
	/// Result obtained successfully
	/// </summary>
	Success,

	/// <summary>
	/// Engine is on cooldown due to too many requests
	/// </summary>
	Cooldown,

	/// <summary>
	/// Obtaining results failed due to an engine error
	/// </summary>
	UnknownError,

	IllegalInput,

	/// <summary>
	/// Engine is unavailable
	/// </summary>
	Unavailable

}

[Flags]
public enum SearchResultFlags
{

	None = 0,

	/// <summary>
	/// Engine returned no results
	/// </summary>
	NoResults = 1 << 0,

	/// <summary>
	/// Result is extraneous
	/// </summary>
	Extraneous = 1 << 1,

}

/// <summary>
/// Root search result returned by a <see cref="BaseSearchEngine"/>
/// </summary>
public class SearchResult : IDisposable, INotifyPropertyChanged
{

	/// <summary>
	/// Engine which returned this result
	/// </summary>
	[JI]
	public BaseSearchEngine Engine { get; }

	// todo: make the engine reference weak

	/// <summary>
	/// Undifferentiated result URL
	/// </summary>
	public Url RawUrl { get; internal set; }

	[JI]
	public bool HasResults
	{
		get
		{
			// return (Results != null && Results.Count != 0);
			return !Flags.HasFlagFast(SearchResultFlags.NoResults);
		}
	}

	public bool IsSuccessful => Status.IsSuccessful();

	/// <summary>
	/// Results; first element should be <see cref="RawResultItem"/>
	/// </summary>
	[NN]
	public List<SearchResultItem> Results { get; }

	[CBN]
	public string ErrorMessage { get; internal set; }

	public SearchResultStatus Status { get; internal set; }

	public SearchResultFlags Flags { get; internal set; }

	[CBN]
	public string Overview { get; internal set; }

	internal SearchResult(BaseSearchEngine bse)
	{
		Engine  = bse;
		Results = [];

		// Results = [GetRawResultItem()];
	}

	public void Update()
	{
		/*
		if (Status.IsError()) {
			return;
		}
		*/

		if (Status.IsError()) {
			return;
		}


		/*if (!any && Status != SearchResultStatus.None) {
			Status = SearchResultStatus.NoResults;
		}
		else {
			Status = SearchResultStatus.Success;
		}*/

	}

	public event PropertyChangedEventHandler PropertyChanged;

	private void OnPropertyChanged([CMN] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private bool SetField<T>(ref T field, T value, [CMN] string propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
			return false;

		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}

	[CBN]
	public SearchResultItem GetBestResult()
	{
		if (Results.Count == 0) {
			// This should never happen so long as results contains the raw item
			Debugger.Break();
			return null;
		}

		return Results.OrderByDescending(static r => r.Similarity)
			.FirstOrDefault(static r => Url.IsValid(r.Url));
	}

	[JI]
	public SearchResultItem RawResultItem
	{
		get
		{
			var rawCache = new SearchResultItem(this, true)
			{
				Url = RawUrl
			};
			return rawCache;
		}
	}

	public override string ToString()
	{
		return $"[{Engine.Name}] {RawUrl} | {Results.Count} | {Status} {ErrorMessage}";
	}

	public void Dispose()
	{
		Debug.WriteLine($"Disposing {Engine.Name} with {Results.Count}", LogCategories.C_VERBOSE);

		foreach (SearchResultItem item in Results) {
			item.Dispose();
		}
	}

}
