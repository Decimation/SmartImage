// Author: Deci | Project: SmartImage.Lib | Name: IResultItem.cs
// Date: 2026/02/08 @ 01:02:26

using Argon;
using CoenM.ImageHash;
using Novus.Streams;
using SmartImage.Lib.Model;
using System.ComponentModel;
using System.Diagnostics;

namespace SmartImage.Lib.Images.Uni;

public interface IResultItem : IDisposable, ISimilarity, IHashable, INotifyPropertyChanged
{

	

}

public interface IUni : IImage, IResultItem
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
	async ValueTask<(bool AllocSourceOk, bool AllocImageOk)> AllocAll(CancellationToken ct)
	{
		bool allocOk    = await AllocSourceAsync(ct);
		bool allocImgOk = false;

		if (allocOk) {
			allocImgOk = await AllocImageAsync(ct);
		}

		return (allocOk, allocImgOk);
	}
}