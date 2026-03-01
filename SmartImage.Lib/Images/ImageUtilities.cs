// ReSharper disable RedundantUsingDirective.Global
// Author: Deci | Project: SmartImage.Lib | Name: ImageUtilities.cs
// Date: 2025/12/27 @ 14:12:45

#region Aliases

global using SizeVdS2 = Kantan.Numeric.SizeVariadic<short>;
global using SizeVdS4 = Kantan.Numeric.SizeVariadic<int>;
global using SizeVdS8 = Kantan.Numeric.SizeVariadic<long>;
global using SizeVdF4 = Kantan.Numeric.SizeVariadic<float>;
global using SizeVdF8 = Kantan.Numeric.SizeVariadic<double>;
global using SizeIS = SixLabors.ImageSharp.Size;

#endregion

using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;
using Kantan.Text;
using SixLabors.ImageSharp.Processing;

namespace SmartImage.Lib.Images;

public static class ImageUtilities
{

	public static IImageHash Hasher { get; set; } = new PerceptualHash();

	public static SizeIS ResizeByFactor(this SizeIS cs, SizeIS newSize)
	{
		int origWidth  = cs.Width;
		int origHeight = cs.Height;

		double widthRatio  = (double) newSize.Width  / origWidth;
		double heightRatio = (double) newSize.Height / origHeight;
		double scale       = Math.Min(widthRatio, heightRatio);

		int newWidth  = (int) (origWidth  * scale);
		int newHeight = (int) (origHeight * scale);

		return new SizeIS(newWidth, newHeight);
	}

	extension(ISImage image)
	{

		public ISImage ResizeByFactor(SizeIS newSize)
		{
			var cs = image.Size.ResizeByFactor(newSize);

			// Resize the image
			var resized = image.Clone(ctx => ctx.Resize(new ResizeOptions()
			{
				Size = cs,
			}));
			return resized;
		}
	}

	public static SizeIS ParseSize(string resText)
	{
		string[] resFull = resText.Split(Strings.Constants.MUL_SIGN);

		int w = -1, h = -1;

		if (resFull.Length == 1 && resFull[0] == resText) {
			const string TIMES_DELIM = "&times;";

			if (resText.Contains(TIMES_DELIM)) {
				resFull = resText.Split(TIMES_DELIM);
			}
		}

		if (resFull.Length == 2) {
			w = Int32.Parse(resFull[0]);
			h = Int32.Parse(resFull[1]);
		}

		return new(w, h);
	}

}