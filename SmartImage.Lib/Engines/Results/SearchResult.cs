using System.ComponentModel;
using System.Diagnostics;
using Kantan.Diagnostics;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Engines.Results;

#nullable disable

/// <summary>
/// Root search result returned by a <see cref="BaseSearchEngine"/>
/// </summary>
public class SearchResult : IDisposable, INotifyPropertyChanged
{

	// TODO: FLATTEN SearchResult to SearchResultItem and eliminate SearchResult ≡ SearchResultItem

	/// <summary>
	/// Engine which returned this result
	/// </summary>
	[JI]
	public BaseSearchEngine Engine { get; }

	// todo: make the engine reference weak

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
		Engine        = bse;
		RawResultItem = GetRawResultItem(rawUrl);
		Results       = [RawResultItem];

		/*m_rawResultItem = new Lazy<SearchResultItem>(() =>
		{
		var rawCache = new SearchResultItem(this, true)
		{
			Url = RawUrl
		};
		return rawCache;
		})*/
		;

		// Results = [GetRawResultItem()];
	}


	private SearchResultItem GetRawResultItem(Url rawUrl)
	{
		return new SearchResultItem(this, true)
		{
			Url = rawUrl
		};
	}

	public virtual void Update()
	{
		if (Status.IsUnknown()) { }

		if (Status.IsError()) {
			return;
		}

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
		// This should never happen so long as results contains the raw item
		Debug.Assert(Results.Count == 0);

		// *? IMPROVE

		return Results.Where(static r => Url.IsValid(r.Url))
			.OrderByDescending(static r => r.Similarity)
			.ThenByDescending(static r => r.Score)
			.FirstOrDefault();
	}

	public override string ToString()
	{
		return $"[{Engine.Name}] {RawUrl} | {Results.Count} | {Status} {ErrorMessage}";
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		Debug.WriteLine($"Disposing {Engine.Name} with {Results.Count}", LogCategories.C_VERBOSE);

		foreach (SearchResultItem item in Results) {
			item.Dispose();
		}
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