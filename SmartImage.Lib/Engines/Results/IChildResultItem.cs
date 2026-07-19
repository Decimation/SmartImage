// Author: Deci | Project: SmartImage.Lib | Name: IChildResultItem.cs
// Date: 2026/06/27 @ 20:06:04

namespace SmartImage.Lib.Engines.Results;

public interface IChildResultItem /*: IResultItem*/
{

	IResultItem Parent { get; }

	bool IsChild { get; }

}