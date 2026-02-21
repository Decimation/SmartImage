// Author: Deci | Project: SmartImage.Lib | Name: IResultItem.cs
// Date: 2026/02/08 @ 01:02:26

using SmartImage.Lib.Model;
using System.ComponentModel;

namespace SmartImage.Lib.Engines.Results;

public interface IResultItem : IDisposable, ISimilarity, IHashable, INotifyPropertyChanged, IUrl
{

	

}