// ReSharper disable RedundantUsingDirective.Global
// Author: Deci | Project: SmartImage.Lib | Name: SizeVariadic.cs
// Date: 2025/08/21 @ 00:08:17

#region Aliases

global using SizeS2N = SmartImage.Lib.Images.SizeVariadic<short>;
global using SizeS4N = SmartImage.Lib.Images.SizeVariadic<int>;
global using SizeS8N = SmartImage.Lib.Images.SizeVariadic<long>;
global using SizeF4N = SmartImage.Lib.Images.SizeVariadic<float>;
global using SizeF8N = SmartImage.Lib.Images.SizeVariadic<double>;
global using SizeIS = SixLabors.ImageSharp.Size;

#endregion

using System.Numerics;
using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming

namespace SmartImage.Lib.Images;

public struct SizeVariadic<T> where T : struct, INumber<T>
{

	public T? Width { get; set; }

	public T? Height { get; set; }

	[MNNW(true, nameof(Width), nameof(Height))]
	public readonly bool IsDimensional => HasWidth && HasHeight;

	[MNNW(true, nameof(Height))]
	private readonly bool HasHeight => Height.HasValue;

	[MNNW(true, nameof(Width))]
	private readonly bool HasWidth => Width.HasValue;

	public SizeVariadic(T? width, T? height)
	{
		Width  = width;
		Height = height;
	}

	public readonly SizeIS ToSize()
	{
		if (!IsDimensional) {
			throw new ArgumentException();
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