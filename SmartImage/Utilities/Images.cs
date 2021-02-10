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
		 * - ImageHash: too high?
		 * - Shipwreck phash: cross correlation
		 */


		public static (int Width, int Height) GetDimensions(string img)
		{
			var bmp = new Bitmap(img);

			return (bmp.Width, bmp.Height);
		}
	}
}