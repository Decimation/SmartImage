// Author: Deci | Project: SmartImage.Lib | Name: IUniImage.cs
// Date: 2026/02/08 @ 02:02:10

using Novus.Streams;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Model;
using System.Threading.Channels;
using JetBrains.Annotations;

namespace SmartImage.Lib.Images.Uni;

public interface IUniImage : IImage, IDisposable, ILength, IAllocSource
{

	long? ILength.Length => Bytes?.Length;

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

	public string Value { get;  }


	/// <summary>
	/// Allocates <see cref="IImage.Image"/> from <see cref="Bytes"/>
	/// </summary>
	Task<bool> AllocImageAsync(CancellationToken ct = default);

	/// <returns><see cref="IAllocSource.AllocSourceAsync"/>, <see cref="IUniImage.AllocImageAsync"/></returns>
	Task<(bool AllocSourceOk, bool AllocImageOk)> AllocAllAsync(CancellationToken ct);

}