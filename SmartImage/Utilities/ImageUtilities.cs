using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartImage.Utilities
{
	public static class ImageUtilities
	{
		//todo

		public static List<bool> GetHash(Bitmap bmpSource, int height)
		{
			List<bool> lResult = new List<bool>();
			//create new image with 16x16 pixel

			Bitmap bmpMin = new Bitmap(bmpSource, new Size(height, height));

			for (int j = 0; j < bmpMin.Height; j++) {
				for (int i = 0; i < bmpMin.Width; i++) {
					//reduce colors to true / false                
					lResult.Add(bmpMin.GetPixel(i, j).GetBrightness() < 0.5f);
				}
			}

			//int equalElements = hash.Zip(hash1, (i, j) => i == j).Count(eq => eq);


			return lResult;
		}

		public static List<bool> GetHash(string bmpSource, int height)
		{
			return GetHash((Bitmap) Image.FromFile(bmpSource), height);
		}

		public static ulong Hash_d(string s, int size = 256)
		{
			//widthAndLength := uint(math.Ceil(math.Sqrt(float64(hashLength)/2.0)) + 1)
			var wl = (int) (Math.Ceiling(Math.Sqrt(((float) size) / 2.0)) + 1);

			Debug.WriteLine($"{wl}");

			Image im = Image.FromFile(s);
			//new Bitmap(9, 8, PixelFormat.Format16bppGrayScale);

			Bitmap c = new Bitmap(im, new Size(wl + 1, wl));


			ulong h = 0;

			// Loop through the images pixels to reset color.
			for (int i = 0; i < c.Width; i++) {
				for (int x = 0; x < c.Height; x++) {
					Color oc        = c.GetPixel(i, x);
					int   grayScale = (int) ((oc.R * 0.3) + (oc.G * 0.59) + (oc.B * 0.11));
					Color nc        = Color.FromArgb(oc.A, grayScale, grayScale, grayScale);
					c.SetPixel(i, x, nc);
				}
			}
			//c = MakeGrayscale3(c);

			// int x, y;
			//
			// for (x = 0; x < c.Width; x++)
			// {
			// 	for (y = 0; y < c.Height; y++)
			// 	{
			// 		Color pixelColor = c.GetPixel(x, y);
			// 		Color newColor   = Color.FromArgb(pixelColor.R, 0, 0);
			// 		c.SetPixel(x, y, newColor); // Now greyscale
			// 	}
			// }

			for (int j = 0; j < wl; j++) {
				for (int k = 0; k < wl; k++) {
					var b   = (c.GetPixel(j, k).R > c.GetPixel(j + 1, k).R);
					var bit = Convert.ToUInt64(b) << (j + k * 8);
					h |= bit;
				}
			}

			return h;
		}
	}
}
