using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Novus.Win32;
using SimpleCore.Console.CommandLine;
using SimpleCore.Net;
using SmartImage.Core;

namespace SmartImage.Utilities
{
	/// <summary>
	/// Image utilities
	/// </summary>
	internal static class Images
	{
		/*
		 * ImageHash - too high
		 * Shipwreck phash - cross correlation
		 */

		public static double? Similarity(string url, string f)
		{
			var m = Network.IdentifyType(url);

			if (m == null) {
				return null;
			}

			var t = Network.IsImage(m);

			Debug.WriteLine($"{url} is image: {t}");


			if (t) {


				// using var s  = Image.FromFile(f);
				// using var s2 = Image.FromStream(new WebClient().OpenRead(url));
				//
				//
				//
				// return Similarity(s, s2);
			}

			return null;
		}

		public static double? Similarity(Image a, Image b)
		{
			return null;
		}

		internal static (int Width, int Height) GetDimensions(string img)
		{
			var bmp = new Bitmap(img);

			return (bmp.Width, bmp.Height);
		}

		internal static bool IsFileValid(string img)
		{
			if (String.IsNullOrWhiteSpace(img)) {
				return false;
			}

			if (!File.Exists(img)) {
				NConsole.WriteError($"File does not exist: {img}");
				return false;
			}

			bool isImageType = FileSystem.ResolveFileType(img).Type == FileType.Image;

			if (!isImageType) {
				return NConsole.ReadConfirmation("File format is not recognized as a common image format. Continue?");
			}

			return true;
		}
	}
}