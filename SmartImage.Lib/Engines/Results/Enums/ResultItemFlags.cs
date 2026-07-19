// Author: Deci | Project: SmartImage.Lib | Name: ResultItemFlags.cs
// Date: 2026/07/18 @ 17:07:10

namespace SmartImage.Lib.Engines.Results.Enums;

[Flags]
public enum ResultItemFlags
{

	None      = 0,
	HasSource = 1 << 0,
	HasImage  = 1 << 1,

}