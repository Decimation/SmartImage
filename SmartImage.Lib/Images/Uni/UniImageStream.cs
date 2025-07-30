// Author: Deci | Project: SmartImage.Lib | Name: UniImageStream.cs
// Date: 2024/07/17 @ 02:07:31

using Microsoft.Extensions.Logging;
using Novus.Streams;
using SixLabors.ImageSharp.PixelFormats;

namespace SmartImage.Lib.Images.Uni;

public class UniImageStream : UniImage
{

	public Stream Stream { get; }

	internal UniImageStream(object value, Stream str)
		: base(value, UniImageType.Stream)
	{
		Stream = str;
	}


	public static bool IsStreamType(object o, out Stream t2)
	{
		t2 = Stream.Null;

		if (o is Stream sz) {
			t2 = sz;
		}

		return t2 != Stream.Null;
	}


	public override async Task<bool> AllocImageAsync(CancellationToken ct = default)
	{
		if (!HasImage) {
			try {

				// Stream     = File.OpenRead(fullName);
				Size  = Stream.Length;
				Image = await ISImage.LoadAsync<Rgba32>(Stream, ct);

			}
			catch (Exception exception) {
				s_logger.LogError(exception, "{Func}", nameof(UniImageFile));
				return false;
			}

		}

		return HasImage;

	}

}