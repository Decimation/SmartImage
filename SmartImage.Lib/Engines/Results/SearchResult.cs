using AngleSharp.Css.Values;
using AngleSharp.Html.Parser;
using JetBrains.Annotations;
using Kantan.Diagnostics;
using SmartImage.Lib.Images;
using SmartImage.Lib.Utilities;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;

namespace SmartImage.Lib.Engines.Results;

#nullable disable

/// <summary>
/// Root search result returned by a <see cref="BaseSearchEngine"/>
/// </summary>
public class SearchResult : IDisposable, INotifyPropertyChanged
{

	// IDEA: FLATTEN SearchResult to SearchResultItem and eliminate SearchResult ≡ SearchResultItem

	/// <summary>
	/// Engine which returned this result
	/// </summary>
	[JI]
	public BaseSearchEngine Engine { get; }

	/// <summary>
	/// Undifferentiated result URL
	/// </summary>
	public Url RawUrl
	{
		get => RawResultItem.Url;
		set => RawResultItem.Url = value;
	}

	[JI]
	public bool HasResults => !Flags.HasFlagFast(SearchResultFlags.NoResults);

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

	// private Lazy<SearchResultItem> m_rawResultItem;

	[JI]
	public SearchResultItem RawResultItem { get; }

	internal SearchResult(BaseSearchEngine bse, Url rawUrl)
	{
		Engine = bse;

		RawResultItem = new SearchResultItem(this, true)
		{
			Url = rawUrl
		};

		Results = [RawResultItem];
	}

	[LinqTunnel]
	public IEnumerable<SearchResultItem> FindGroups(SearchResultItem sri)
	{
		return Results.Where(k => k.Parent == sri);
	}

	public virtual void Update()
	{
		if (Status.IsUnknown()) { }

		if (Status.IsError()) {
			return;
		}

	}


#region

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

#endregion

	[CBN]
	public SearchResultItem GetBestResult()
	{
		// TODO *? IMPROVE

		return Results.Where(static r => Url.IsValid(r.Url))
			.OrderByDescending(static r => r.Similarity)
			.ThenByDescending(static r => r.Score)
			.FirstOrDefault();
	}

	/*public int Index(SearchResultItem item, SearchResultItem scn)
	{
		int root;
		int scKi  = 0;
		int scKi2 = 0;

		root = Results.IndexOf(item);

		// scKi = ScannedResults.IndexOf(item);
		if (scn != null) {
			scKi = 1;
		}

		/*foreach ((SearchResultItem key, SearchResultItem[] value) in ScannedResults) {
			if (key == item) {
				scKi2 = Array.IndexOf(value, scn);

				/*if (scKi2==0) {
					scKi2++;
				}#2#
				if (scKi2==-1) {
					// scKi2=0;
				}

				// break;
			}
			else {
				scKi += value.Length;

			}

			// scKi += value.Length;

		}#1#


		/*if (ScannedResults.TryGetValue(sri, out SearchResultItem[] sci)) {
			var i = Array.IndexOf(sci, sri);
			root += i == -1 ? 0 : i;
		}

		for (int i = 0; i < ScannedResults.Count; i++) { }#1#

		return root + scKi + scKi2;

	}

	public int Index(SearchResultItem sri)
	{
		var root = Results.IndexOf(sri.Parent);

		// var scKi  = ScannedResults.IndexOf(sri);
		var scKi2 = 0;

		/*if (scKi != -1) {
			for (int sc = 0; sc < scKi; sc++) {
				var scannedItems = ScannedResults[sri];
				var scI2         = Array.IndexOf(scannedItems, sri);

				if (scI2 != -1) {
					scKi2 += scI2;
				}
			}
		}
		else {
			scKi = 0;
		}#1#


		/*if (ScannedResults.TryGetValue(sri, out SearchResultItem[] sci)) {
			var i = Array.IndexOf(sci, sri);
			root += i == -1 ? 0 : i;
		}

		for (int i = 0; i < ScannedResults.Count; i++) { }#1#

		// return root + scKi + scKi2;

		return root + scKi2;
	}*/

	/*
	public int AggIdx(SearchResultItem sri)
	{
		var resIdx=Results.IndexOf(sri);

	}
	*/

	public override string ToString()
	{
		return $"[{Engine.Name}] {RawUrl} | {Results.Count} | {Status} {ErrorMessage}";
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		Debug.WriteLine($"Disposing {Engine.Name} with {Results.Count}", LogCategories.C_VERBOSE);

		foreach (SearchResultItem item in Results) {
			/*if (ScannedResults.TryGetValue(item, out var scanned)) {
				foreach (var sci in scanned) {
					sci.Dispose();
				}
			}*/

			item.Dispose();
		}

		// ScannedResults.Clear();

	}

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