// ReSharper disable RedundantUsingDirective.Global
// Author: Deci | Project: SmartImage.Lib | Name: SizeTN.cs
// Date: 2025/08/21 @ 00:08:17

#region Aliases

global using SizeS2N = SmartImage.Lib.Images.SizeTN<short>;
global using SizeS4N = SmartImage.Lib.Images.SizeTN<int>;
global using SizeS8N = SmartImage.Lib.Images.SizeTN<long>;
global using SizeF4N = SmartImage.Lib.Images.SizeTN<float>;
global using SizeF8N = SmartImage.Lib.Images.SizeTN<double>;
global using SizeIS = SixLabors.ImageSharp.Size;

#endregion

using System.Numerics;
using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming

namespace SmartImage.Lib.Images;

public struct SizeTN<T> where T : struct, INumber<T>
{

	public T? Width { get; set; }

	public T? Height { get; set; }

	[MNNW(true, nameof(Width), nameof(Height))]
	public readonly bool IsComplete => HasWidth && HasHeight;

	[MNNW(true, nameof(Height))]
	private readonly bool HasHeight => Height.HasValue;

	[MNNW(true, nameof(Width))]
	private readonly bool HasWidth => Width.HasValue;

	public SizeTN(T? width, T? height)
	{
		Width  = width;
		Height = height;
	}

	public readonly SizeIS ToSize()
	{
		if (IsComplete) {
			throw new InvalidOperationException();
		}

		var wv = Width.Value;
		var hv = Height.Value;

		int iwv = default;
		int ihv = default;

		if (T.IsInteger(wv)) {
			iwv = Unsafe.As<T, int>(ref wv);
			ihv = Unsafe.As<T, int>(ref hv);
		}

		//todo

		return new SizeIS(iwv, ihv);
	}

}