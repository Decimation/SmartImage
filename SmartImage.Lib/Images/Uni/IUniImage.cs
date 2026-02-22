// Author: Deci | Project: SmartImage.Lib | Name: IUniImage.cs
// Date: 2026/02/08 @ 02:02:10

using Novus.Streams;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Model;

namespace SmartImage.Lib.Images.Uni;

public interface IUniImage : IImage, IDisposable, ILength, IUrl
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
	Stream GetStream()
	{
		return UniImage.MemMgr.GetStream(Bytes.GetHashCode().ToString(), Bytes);
	}

	/// <summary>
	/// Allocates <see cref="Bytes"/>
	/// </summary>
	[MNNW(true, nameof(Bytes))]
	ValueTask<bool> AllocSourceAsync(CancellationToken ct = default);

	/// <summary>
	/// Allocates <see cref="IImage.Image"/> from <see cref="Bytes"/>
	/// </summary>
	[MNNW(true, nameof(Image))]
	async ValueTask<bool> AllocImageAsync(CancellationToken ct = default)
	{
		if (this.Url == null) {
			return false;
		}

		if (HasImage) {
			return true;
		}

		bool allocImgOk = false;
		var  allocOk    = await AllocSourceAsync(ct);

		if (allocOk) {
			//todo?
			allocImgOk = await AllocImageAsync(ct);
		}

		if (allocImgOk) {

			Width  ??= Image.Width;
			Height ??= Image.Height;

			// Root.Results.Add(this);
		}
		else { }

		return HasImage;
	}


	/// <returns><see cref="AllocSourceAsync"/>, <see cref="AllocImageAsync"/></returns>
	ValueTask<(bool AllocSourceOk, bool AllocImageOk)> AllocAll(CancellationToken ct);

}