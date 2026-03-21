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

	public bool IsRaw { get; }

	public int Index {get;}
}