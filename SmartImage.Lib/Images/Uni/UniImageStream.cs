// Author: Deci | Project: SmartImage.Lib | Name: UniImageStream.cs
// Date: 2024/07/17 @ 02:07:31

using Novus.Streams;

namespace SmartImage.Lib.Images.Uni;

public class UniImageStream : UniImage
{

	internal UniImageStream(object value, Stream str)
		: base(value, str, UniImageType.Stream) { }


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

	public override string WriteToFile(string fn = null)
		=> WriteStreamToFile(fn);

}