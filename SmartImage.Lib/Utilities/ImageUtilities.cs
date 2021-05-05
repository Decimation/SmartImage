using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using SimpleCore.Net;

// ReSharper disable UnusedMember.Global

namespace SmartImage.Lib.Utilities
{
	public static class ImageUtilities
	{
		//todo

		/*
		 * https://stackoverflow.com/questions/35151067/algorithm-to-compare-two-images-in-c-sharp
		 * https://stackoverflow.com/questions/23931/algorithm-to-compare-two-images
		 * https://github.com/aetilius/pHash
		 * http://hackerfactor.com/blog/index.php%3F/archives/432-Looks-Like-It.html
		 * https://github.com/ishanjain28/perceptualhash
		 * https://github.com/Tom64b/dHash
		 * https://github.com/Rayraegah/dhash
		 * https://tineye.com/faq#how
		 * https://github.com/CrackedP0t/Tidder/
		 * https://github.com/taurenshaman/imagehash
		 */

		/*public static HtmlNode Index(this HtmlNode node, params int[] i)
		{
			if (!i.Any()) {
				return node;
			}
			if (i.First() < node.ChildNodes.Count) {
				return node.ChildNodes[i.First()].Index(i.Skip(1).ToArray());
			}

			return node;
		}*/

		public static (int w, int h) GetResolution(string s)
		{
			using var bmp = Image.FromFile(s);

			return (bmp.Width, bmp.Height);
		}

		public static string GetResolutionType(int w, int h)
		{
			/*
			 *	Other			W < 1280	
			 *	[HD, FHD)		[1280, 1920)	1280 <= W < 1920	W: >= 1280 < 1920
			 *	[FHD, QHD)		[1920, 2560)	1920 <= W < 2560	W: >= 1920 < 2560
			 *	[QHD, UHD)		[2560, 3840)	2560 <= W < 3840	W: >= 2560 < 3840
			 *	[UHD, ∞)											W: >= 3840
			 */

			return w switch
			{
				< 1280             => "Other",
				>= 1280 and < 1920 => "HD",
				>= 1920 and < 2560 => "FHD",
				>= 2560 and < 3840 => "QHD",
				>= 3840            => "UHD",
			};
		}

		public static bool IsDirectImage(string value)
		{
			return MediaTypes.IsDirect(value, MimeType.Image);
		}

		/*public static List<bool> GetHash(Bitmap bmpSource, int height)
		{
			var lResult = new List<bool>();
			//create new image with 16x16 pixel

			var bmpMin = new Bitmap(bmpSource, new Size(height, height));

			for (int j = 0; j < bmpMin.Height; j++)
			{
				for (int i = 0; i < bmpMin.Width; i++)
				{
					//reduce colors to true / false                
					lResult.Add(bmpMin.GetPixel(i, j).GetBrightness() < 0.5f);
				}
			}

			//int equalElements = hash.Zip(hash1, (i, j) => i == j).Count(eq => eq);


			return lResult;
		}

		public static List<bool> GetHash(string bmpSource, int height)
		{
			return GetHash((Bitmap)Image.FromFile(bmpSource), height);
		}

		public static ulong Hash_d(string s, int size = 256)
		{
			//widthAndLength := uint(math.Ceil(math.Sqrt(float64(hashLength)/2.0)) + 1)
			var wl = (int)(Math.Ceiling(Math.Sqrt(((float)size) / 2.0)) + 1);

			//https://stackoverflow.com/questions/2265910/convert-an-image-to-grayscale

			Debug.WriteLine($"{wl}");

			Image im = Image.FromFile(s);
			//new Bitmap(9, 8, PixelFormat.Format16bppGrayScale);

			Bitmap c = new Bitmap(im, new Size(wl + 1, wl));


			ulong h = 0;

			// Loop through the images pixels to reset color.
			for (int i = 0; i < c.Width; i++)
			{
				for (int x = 0; x < c.Height; x++)
				{
					Color oc = c.GetPixel(i, x);
					int grayScale = (int)((oc.R * 0.3) + (oc.G * 0.59) + (oc.B * 0.11));
					Color nc = Color.FromArgb(oc.A, grayScale, grayScale, grayScale);
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

			for (int j = 0; j < wl; j++)
			{
				for (int k = 0; k < wl; k++)
				{
					var b = (c.GetPixel(j, k).R > c.GetPixel(j + 1, k).R);
					var bit = Convert.ToUInt64(b) << (j + k * 8);
					h |= bit;
				}
			}

			return h;
		}*/
		public static string ResolveDirectLink(string s)
		{
			//todo: WIP
			string d = "";

			try {
				var    uri  = new Uri(s);
				string host = uri.Host;


				var doc  = new HtmlDocument();
				var html = Network.GetSimpleResponse(s);

				if (host.Contains("danbooru")) {
					Debug.WriteLine("danbooru");


					var jObject = JObject.Parse(html.Content);

					d = (string) jObject["file_url"]!;


					return d;
				}

				doc.LoadHtml(html.Content);

				string sel = "//img";

				var nodes = doc.DocumentNode.SelectNodes(sel);

				if (nodes == null) {
					return null;
				}

				Debug.WriteLine($"{nodes.Count}");
				Debug.WriteLine($"{nodes[0]}");


			}
			catch (Exception e) {
				Debug.WriteLine($"direct {e.Message}");
				return d;
			}


			return d;
		}
	}
}