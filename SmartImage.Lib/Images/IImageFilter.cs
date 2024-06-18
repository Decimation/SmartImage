// Author: Deci | Project: SmartImage.Lib | Name: IImageFilter.cs
// Date: 2024/05/02 @ 11:05:28

namespace SmartImage.Lib.Images;
// todo

public interface IImageFilter
{

	public string[] Blacklist { get; }

	public bool Refine(string b);

	public bool Predicate(UniImage us);

}