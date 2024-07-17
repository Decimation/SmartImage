// Author: Deci | Project: SmartImage.Lib | Name: UniImageUnknown.cs
// Date: 2024/07/17 @ 02:07:36

namespace SmartImage.Lib.Images.Uni;

public class UniImageUnknown : UniImage, IUniImage
{

	internal UniImageUnknown() : base(null, Stream.Null, UniImageType.Unknown) { }

	static IUniImage IUniImage.TryCreate(object o, CancellationToken ct = default)
	{
		return new UniImageUnknown();
	}

	public override async ValueTask<bool> Alloc(CancellationToken ct = default)
	{
		return false;
	}

}