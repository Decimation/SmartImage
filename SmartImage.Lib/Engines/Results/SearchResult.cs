using JetBrains.Annotations;
using Kantan.Diagnostics;
using SmartImage.Lib.Utilities;
using System.ComponentModel;
using System.Diagnostics;
using SmartImage.Lib.Model;

namespace SmartImage.Lib.Engines.Results;

#nullable disable

/// <summary>
/// Root search result returned by a <see cref="BaseSearchEngine"/>
/// </summary>
public class SearchResult : IDisposable, INotifyPropertyChanged
{

	// IDEA: FLATTEN SearchResult to SearchResultItem and eliminate SearchResult ≡ SearchResultItem?

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
	public List<IResultItem> Results { get; }

	// TODO: IResultItem


	[CBN]
	public string ErrorMessage { get; internal set; }

	public SearchResultStatus Status { get; internal set; }

	public SearchResultFlags Flags { get; internal set; }

	[CBN]
	public string Overview { get; internal set; }


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
	public IResultItem GetBestResult()
	{
		// TODO *? IMPROVE

		return Results.Where(static r => Url.IsValid(r.Url))
		              .OrderByDescending(static r => r.Similarity)
		              .ThenByDescending(static r => r is SearchResultItem sri ? sri.Score : 0)
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
			/*if (ScannedResults.TryGetValue(item, out var scanned)) {
				foreach (var sci in scanned) {
					sci.Dispose();
				}
			}*/

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