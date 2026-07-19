// Author: Deci | Project: SmartImage.Lib | Name: IAlloc.cs
// Date: 2026/06/13 @ 01:06:37

namespace SmartImage.Lib.Images.Alloc;

public interface IAllocSource
{

	byte[] Source { get; }

	[MNNW(true, nameof(Source))]
	bool HasSource { get; }

	string Value { get; }

	[MNNW(true, nameof(HasSource))]
	Stream GetSource();

	/// <summary>
	/// Allocates <see cref="Source"/> (<see cref="GetSource"/>)
	/// </summary>
	Task<bool> AllocSourceAsync(CancellationToken ct = default);

}