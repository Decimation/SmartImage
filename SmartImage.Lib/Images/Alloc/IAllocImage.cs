// Author: Deci | Project: SmartImage.Lib | Name: IAllocImage.cs
// Date: 2026/02/08 @ 02:02:10

using SmartImage.Lib.Model;

namespace SmartImage.Lib.Images.Alloc;

public interface IAllocImage : IImage, ILength, IAllocSource, IDisposable
{

	long? ILength.Length => Source?.Length;

	int? IDimensions.Width
	{
		get => Image?.Width;
		set { }
	}

	int? IDimensions.Height
	{
		get => Image?.Width;
		set { }
	}


	/// <summary>
	/// Allocates <see cref="IImage.Image"/> from <see cref="IAllocSource.Source"/>
	/// </summary>
	Task<bool> AllocImageAsync(CancellationToken ct = default);

	// AllocSourceFlags Flags { get; }
}