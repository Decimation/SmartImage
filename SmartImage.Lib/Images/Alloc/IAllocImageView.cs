// Author: Deci | Project: SmartImage.Lib | Name: IAllocImageView.cs
// Date: 2026/07/18 @ 18:07:02

namespace SmartImage.Lib.Images.Alloc;

public interface IAllocImageView<out TAllocImage> where TAllocImage : IAllocImage
{

	TAllocImage AllocImage { get; }

}