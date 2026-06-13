// Author: Deci | Project: SmartImage.Lib | Name: IImageScanner.cs
// Date: 2026/06/13 @ 00:06:59

using SmartImage.Lib.Images.Uni;
using System.Threading.Channels;

namespace SmartImage.Lib.Images;

public interface IImageScanner<T> where T : IUniImage
{

	ValueTask<bool> ScanAsync(ChannelWriter<T> cw, Func<Url, T> f, CancellationToken ct = default);

}

cl