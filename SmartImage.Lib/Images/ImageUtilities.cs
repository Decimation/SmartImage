// Author: Deci | Project: SmartImage.Lib | Name: ImageUtilities.cs
// Date: 2025/12/27 @ 14:12:45

using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;
using Kantan.Text;
using Novus.Utilities;
using SixLabors.ImageSharp.Processing;
using SmartImage.Lib.Images;

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

	public static SizeS4N ParseResolution(string resText)
	{
		string[] resFull = resText.Split(Strings.Constants.MUL_SIGN);

		int? w = null, h = null;

		if (resFull.Length == 1 && resFull[0] == resText) {
			const string TIMES_DELIM = "&times;";

			if (resText.Contains(TIMES_DELIM)) {
				resFull = resText.Split(TIMES_DELIM);
			}
		}

		if (resFull.Length == 2) {
			w = int.Parse(resFull[0]);
			h = int.Parse(resFull[1]);
		}

		return new(w, h);
	}

}