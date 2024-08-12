// Author: Deci | Project: SmartImage.Lib | Name: UniImageUnknown.cs
// Date: 2024/07/17 @ 02:07:36

namespace SmartImage.Lib.Images.Uni;

public class UniImageUnknown : UniImage
{

	internal UniImageUnknown() : base(null, UniImageType.Unknown) { }

	

	public override async ValueTask<bool> Alloc(CancellationToken ct = default)
	{
		return false;
	}

}