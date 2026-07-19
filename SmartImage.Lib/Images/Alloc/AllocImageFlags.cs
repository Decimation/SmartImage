using System;
using System.Collections.Generic;
using System.Text;

namespace SmartImage.Lib.Images.Alloc;

[Flags]
public enum AllocImageFlags
{

	None = 0,

	/// <summary>
	/// <see cref="IAllocSource.Source"/>
	/// </summary>
	HasSource = 1 << 0,

	/// <summary>
	/// <see cref="IAllocImage.Image"/> (<see cref="IImage.Image"/>)
	/// </summary>
	HasImage = 1 << 1,

	Failed   = 1 << 2

}