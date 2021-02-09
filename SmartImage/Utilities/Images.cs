using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
//using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Novus.Win32;
using SimpleCore.Cli;
using SimpleCore.Net;
using SmartImage.Core;

namespace SmartImage.Utilities
{
	/// <summary>
	/// Image utilities
	/// </summary>
	public static class Images
	{
		/* Image comparison algorithms:
		 * - ImageHash: too high
		 * - Shipwreck phash: cross correlation
		 */


		public static (int Width, int Height) GetDimensions(string img)
		{
			var bmp = new Bitmap(img);

			return (bmp.Width, bmp.Height);
		}

		public static bool IsFileValid(string img)
		{
			if (String.IsNullOrWhiteSpace(img)) {
				return false;
			}
			
			/*bool isUri = Network.IsUri(img);

			bool isFile = File.Exists(img);

			Debug.WriteLine($"{isUri} {isFile}");

			if (isFile) {
				
				bool isImageType = FileSystem.ResolveFileType(img).Type == FileType.Image;

				if (!isImageType)
				{
					return NConsole.ReadConfirmation("File format is not recognized as a common image format. Continue?");
				}
				
			}
			else if (isUri) {

			}*/
			

			

			return true;
		}
	}
}