// Author: Deci | Project: SmartImage.Lib | Name: IAllocImageView.cs
// Date: 2026/07/18 @ 18:07:02

using SmartImage.Lib.Images.Alloc;

namespace SmartImage.Lib.Engines.Results;

public interface IAllocImageView<out T> where T:IAllocImage
{

	T AllocImage { get; }

}