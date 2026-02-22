// Author: Deci | Project: SmartImage.Lib | Name: IUniImage.cs
// Date: 2026/02/08 @ 02:02:10

using Novus.Streams;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Model;

namespace SmartImage.Lib.Images.Uni;

public interface IUniImage : IImage, IDisposable
{

	byte[] Bytes { get; }

	[MNNW(true, nameof(Bytes))]
	bool HasBytes { get; }

	Stream GetStream();

	/// <summary>
	/// Allocates <see cref="Bytes"/>
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


	[MNNW(true, nameof(Image))]
	public static async ValueTask<bool> AllocImageAsync<T>(T img, CancellationToken ct = default) where T : IUniImage, IUrl
	{
		if (img.Url == null) {
			return false;
		}

		if (img.HasImage) {
			return true;
		}

		bool allocImgOk = false;
		var  allocOk    = await img.AllocSourceAsync(ct);

		if (allocOk) {
			//todo?
			allocImgOk = await img.AllocImageAsync(ct);
		}

		if (allocImgOk) {

			img.Width  ??= img.Image.Width;
			img.Height ??= img.Image.Height;

			// Root.Results.Add(this);
		}
		else { }

		return img.HasImage;
	}

}