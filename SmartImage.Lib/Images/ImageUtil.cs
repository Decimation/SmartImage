// using System.Drawing;

// using System.Drawing;

// using System.Windows;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

using Color = SixLabors.ImageSharp.Color;
using Size = SixLabors.ImageSharp.Size;

namespace SmartImage.Lib.Images;

public static class ImageUtil
{

	/*
	public static double CompareImages(ISImage inputImage1, ISImage inputImage2, int tolerance)
	{
		var image1 = inputImage1.CloneAs<Rgba32>();
		var image2 = inputImage2.CloneAs<Rgba32>();

		int           image1Size = image1.Width * image1.Height;
		int           image2Size = image2.Width * image2.Height;
		Image<Rgba32> image3;

		if (image1Size > image2Size) {
			image1.Mutate(x => x.Resize(image2.Size));
			image3 = new Image<Rgba32>(image2.Width, image2.Height);
		}
		else {
			image2.Mutate(x => x.Resize(image1.Size));
			image3 = new Image<Rgba32>(image1.Width, image1.Height);
		}

		for (int x = 0; x < image1.Width; x++) {
			for (int y = 0; y < image1.Height; y++) {
				var color1 = image1[x, y];
				var color2 = image2[x, y];
				int    r      = Math.Abs(color1.R - color2.R);
				int    g      = Math.Abs(color1.G - color2.G);
				int    b      = Math.Abs(color1.B - color2.B);
				image3[x, y] = new Rgba32(r, g, b);
			}
		}

		int difference = 0;

		for (int x = 0; x < image1.Width; x++) {
			for (int y = 0; y < image1.Height; y++) {
				var color1 = image3[x, y];
				int    media  = (color1.R + color1.G + color1.B) / 3;

				if (media > tolerance)
					difference++;
			}
		}

		double usedSize = image1Size > image2Size ? image2Size : image1Size;
		double result   = difference * 100 / usedSize;
		return result;
	}

	public static double CalculateMse(Image<Rgba32> image1, Image<Rgba32> image2)
	{
		int    width  = Math.Min((int) image1.Width, (int) image2.Width);
		int    height = Math.Min((int) image1.Height, (int) image2.Height);
		double mse    = 0;

		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {

				var pixel1 = image1[x,y];
				var pixel2 = image2[x,y];
				
				double diffR = pixel1.R - pixel2.R;
				double diffG = pixel1.G - pixel2.G;
				double diffB = pixel1.B - pixel2.B;

				mse += (diffR * diffR + diffG * diffG + diffB * diffB) / 3.0;
			}
		}

		mse /= width * height;
		return mse;
	}*/

}