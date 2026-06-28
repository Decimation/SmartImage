// Author: Deci | Project: SmartImage.Lib | Name: ISubResultItem.cs
// Date: 2026/06/27 @ 20:06:04

namespace SmartImage.Lib.Engines.Results;

public interface ISubResultItem : IResultItem
{

	IResultItem Parent { get; }

	bool IsChild { get; }

}