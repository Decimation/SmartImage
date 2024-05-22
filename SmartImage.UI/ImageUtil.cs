using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SmartImage.UI
{
	public static class ImageUtil
	{

		public static Bitmap BitmapImage2Bitmap(this BitmapImage bitmapImage)
		{
			// BitmapImage bitmapImage = new BitmapImage(new Uri("../Images/test.png", UriKind.Relative));

			using (MemoryStream outStream = new MemoryStream()) {
				BitmapEncoder enc = new BmpBitmapEncoder();
				enc.Frames.Add(BitmapFrame.Create(bitmapImage));
				enc.Save(outStream);
				Bitmap bitmap = new Bitmap(outStream);

				return new Bitmap(bitmap);
			}
		}

		public static double CompareImages(Bitmap inputImage1, Bitmap inputImage2, int tolerance)
		{
			Bitmap Image1     = new Bitmap(inputImage1, new Size(512, 512));
			Bitmap Image2     = new Bitmap(inputImage2, new Size(512, 512));
			int    Image1Size = Image1.Width * Image1.Height;
			int    Image2Size = Image2.Width * Image2.Height;
			Bitmap Image3;

			if (Image1Size > Image2Size) {
				Image1 = new Bitmap(Image1, Image2.Size);
				Image3 = new Bitmap(Image2.Width, Image2.Height);
			}
			else {
				Image1 = new Bitmap(Image1, Image2.Size);
				Image3 = new Bitmap(Image2.Width, Image2.Height);
			}

			for (int x = 0; x < Image1.Width; x++) {
				for (int y = 0; y < Image1.Height; y++) {
					Color Color1 = Image1.GetPixel(x, y);
					Color Color2 = Image2.GetPixel(x, y);
					int   r      = Color1.R > Color2.R ? Color1.R - Color2.R : Color2.R - Color1.R;
					int   g      = Color1.G > Color2.G ? Color1.G - Color2.G : Color2.G - Color1.G;
					int   b      = Color1.B > Color2.B ? Color1.B - Color2.B : Color2.B - Color1.B;
					Image3.SetPixel(x, y, Color.FromArgb(r, g, b));
				}
			}

			int Difference = 0;

			for (int x = 0; x < Image1.Width; x++) {
				for (int y = 0; y < Image1.Height; y++) {
					Color Color1 = Image3.GetPixel(x, y);
					int   Media  = (Color1.R + Color1.G + Color1.B) / 3;

					if (Media > tolerance)
						Difference++;
				}
			}

			double UsedSize = Image1Size > Image2Size ? Image2Size : Image1Size;
			double result   = Difference * 100 / UsedSize;
			return Difference * 100 / UsedSize;
		}

		public static double CalculateMSE(Bitmap image1, Bitmap image2)
		{
			int    width  = Math.Min(image1.Width, image2.Width);
			int    height = Math.Min(image1.Height, image2.Height);
			double mse    = 0;

			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					Color pixel1 = image1.GetPixel(x, y);
					Color pixel2 = image2.GetPixel(x, y);

					double diffR = pixel1.R - pixel2.R;
					double diffG = pixel1.G - pixel2.G;
					double diffB = pixel1.B - pixel2.B;

					mse += (diffR * diffR + diffG * diffG + diffB * diffB) / 3.0;
				}
			}

			mse /= (width * height);
			return mse;
		}

	}
}
