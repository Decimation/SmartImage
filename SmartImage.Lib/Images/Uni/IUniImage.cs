// Author: Deci | Project: SmartImage.Lib | Name: IUniImage.cs
// Date: 2026/02/08 @ 02:02:10

using Novus.Streams;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Model;
using System.Threading.Channels;

namespace SmartImage.Lib.Images.Uni;

public interface IUniImage : IImage, IDisposable, ILength
{

	byte[] Bytes { get; }

	[MNNW(true, nameof(Bytes), nameof(Length))]
	bool HasBytes => Bytes != null;

	long? ILength.Length => Bytes?.Length;

	int? ISize.Width
	{
		get => Image?.Width;
		set { }
	}

	int? ISize.Height
	{
		get => Image?.Width;
		set { }
	}

	[MURV]
	Stream GetSourceStream();

	/// <summary>
	/// Allocates <see cref="Bytes"/> (<see cref="GetSourceStream"/>)
	/// </summary>
	[MNNW(true, nameof(Bytes))]
	ValueTask<bool> AllocSourceAsync(CancellationToken ct = default);

	/// <summary>
	/// Allocates <see cref="IImage.Image"/> from <see cref="Bytes"/>
	/// </summary>
	[MNNW(true, nameof(Image))]
	ValueTask<bool> AllocImageAsync(CancellationToken ct = default);


	/// <returns><see cref="AllocSourceAsync"/>, <see cref="AllocImageAsync"/></returns>
	ValueTask<(bool AllocSourceOk, bool AllocImageOk)> AllocAll(CancellationToken ct);

}