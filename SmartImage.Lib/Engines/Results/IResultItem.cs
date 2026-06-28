// Author: Deci | Project: SmartImage.Lib | Name: IResultItem.cs
// Date: 2026/02/08 @ 01:02:26

using SmartImage.Lib.Model;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.Formats;

namespace SmartImage.Lib.Engines.Results;

public interface IResultItem : IResultMetadata, IDisposable, ISimilarity, IHashable, IUrl, INotifyPropertyChanged
{

	SearchResult Root { get; }

	bool IsRaw { get; }
}