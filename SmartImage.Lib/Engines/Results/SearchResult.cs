using JetBrains.Annotations;
using Kantan.Diagnostics;
using SmartImage.Lib.Utilities;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using SmartImage.Lib.Model;
using SmartImage.Lib.Engines.Search.Base;
using SmartImage.Lib.Images.Uni;

namespace SmartImage.Lib.Engines.Results;

#nullable disable

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

	/// <summary>
	/// Undifferentiated result URL
	/// </summary>
	public Url RawUrl
	{
		get => RawResultItem.Url;
		internal set => RawResultItem.Url = value;
	}

	[JI]
	public bool HasResults => Results.Any();

	[CBN]
	public string ErrorMessage { get; internal set; }

	public SearchResponseFlags ResponseFlags { get; internal set; }

	[CBN]
	public string Overview { get; internal set; }

	/// <summary>
	/// Results
	/// </summary>
	/// <remarks>First element should be <see cref="RawResultItem"/></remarks>
	[NN]
	public List<IResultItem> Results { get; }

	

	[JI]
	public SearchResultItem RawResultItem { get; }


	internal SearchResult(BaseSearchEngine bse, Url rawUrl)
	{
		Engine = bse;

		RawResultItem = new SearchResultItem(this, true)
		{
			Url = rawUrl
		};

		Results      = [RawResultItem];
	}

#region

	public event PropertyChangedEventHandler PropertyChanged;

	protected virtual void OnPropertyChanged([CMN] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	protected virtual bool SetField<T>(ref T field, T value, [CMN] string propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
			return false;

		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}

#endregion

	[CBN]
	public virtual IResultItem GetBestResult()
	{
		// TODO *? IMPROVE

		return Results.Where(static r => Url.IsValid(r.Url))
		              .OrderByDescending(static r => r.Similarity)
		              .ThenByDescending(static r => r is SearchResultItem sri ? sri.Score : 0)
		              .FirstOrDefault();
	}

	public override string ToString()
	{
		return $"[{Engine.Name}] {RawUrl} | {Results.Count} | {ResponseFlags} {ErrorMessage}";
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		Debug.WriteLine($"Disposing {Engine.Name} with {Results.Count}", LogCategories.C_VERBOSE);

		foreach (var item in Results) {
			/*if (ScannedResults.TryGetValue(item, out var scanned)) {
				foreach (var sci in scanned) {
					sci.Dispose();
				}
			}*/

			item.Dispose();
		}
	}

}