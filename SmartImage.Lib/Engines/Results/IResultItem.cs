// Author: Deci | Project: SmartImage.Lib | Name: IResultItem.cs
// Date: 2026/02/08 @ 01:02:26

using SmartImage.Lib.Model;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.Formats;

namespace SmartImage.Lib.Engines.Results;

public interface IResultItem : IDisposable, ISimilarity, IHashable, IUrl, IResultMetadata, INotifyPropertyChanged
{

	SearchResult Root { get; }

	IResultItem Parent { get; }

	bool IsChild { get; }

	bool IsRaw { get; }

	// public int Index { get; }

	/*
	/// <summary>
	/// Index 1
	/// If <see cref="IsChild"/> (<see cref="ScannedResultItem"/>) scan index ++
	/// </summary>
	public int ResultsIndex => IsChild ? Parent.Root.Results.IndexOf(Parent) : Root.Results.IndexOf(this);
	*/

	/*public int GetIndex1()
	{
		var root   = ResultsIndex;
		var scnIdx = IsChild ? 1 : 0;
		var t      = 0;

		var seg = Parent.Root.Results[0..root].OfType<SearchResultItem>();

		foreach (var sri in seg) {
			var scnItm  = sri.ScannedItems;
			var scnIdx2 = scnItm.IndexOf(this);

			if (scnIdx2 == -1) {
				t += scnItm.Count;
			}
			else {
				t += scnIdx2;
			}
		}

		return root + scnIdx + t;
	}*/

	/*
	public static int GetIndex(IResultItem item)
	{
		var rootIdx = item.Root.Results.IndexOf(item.Parent);
		var idx     = 0;

		for (int i = 0; i < rootIdx; i++) {
			var item1 = item.Root.Results[i];
			
			//
		}

		return idx;
	}
	*/

}