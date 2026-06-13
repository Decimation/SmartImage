// Author: Deci | Project: SmartImage.Lib | Name: IAlloc.cs
// Date: 2026/06/13 @ 01:06:37

using SmartImage.Lib.Model;

namespace SmartImage.Lib.Images.Uni;

public interface IAllocSource
{

	byte[] Bytes { get; }

	[MNNW(true, nameof(Bytes))]
	bool HasBytes { get; }

	[MNNW(true, nameof(HasBytes))]
	Stream GetSource();

	/// <summary>
	/// Allocates <see cref="Bytes"/> (<see cref="GetSource"/>)
	/// </summary>
	Task<bool> AllocSourceAsync(CancellationToken ct = default);

}