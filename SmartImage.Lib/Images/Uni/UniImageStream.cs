// Author: Deci | Project: SmartImage.Lib | Name: UniImageStream.cs
// Date: 2024/07/17 @ 02:07:31

using SixLabors.ImageSharp.Formats;

namespace SmartImage.Lib.Images.Uni;

public class UniImageStream : UniImage, IUniImage
{

	internal UniImageStream(object value, Stream str, IImageFormat format = null)
		: base(value, str, UniImageType.Stream, format) { }

	static IUniImage IUniImage.TryCreate(object o, CancellationToken ct = default)
	{
		if (IsStreamType(o, out var str)) {
			return new UniImageStream(o, str);
		}

		return null;
	}

	public static bool IsStreamType(object o, out Stream t2)
	{
		t2 = Stream.Null;

		if (o is Stream sz) {
			t2 = sz;
		}

		return t2 != Stream.Null;
	}

	public override async ValueTask<bool> Alloc(CancellationToken ct = default)
	{
		return HasStream;
	}

}