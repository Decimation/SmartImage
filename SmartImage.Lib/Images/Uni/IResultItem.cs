// Author: Deci | Project: SmartImage.Lib | Name: IResultItem.cs
// Date: 2026/02/08 @ 01:02:26

using Argon;
using CoenM.ImageHash;
using Novus.Streams;
using SmartImage.Lib.Model;
using System.ComponentModel;
using System.Diagnostics;

namespace SmartImage.Lib.Images.Uni;

public interface IResultItem : IDisposable, ISimilarity, IHashable, INotifyPropertyChanged
{

	

}