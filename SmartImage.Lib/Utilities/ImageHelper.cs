using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Io;
using AngleSharp.XPath;
using Newtonsoft.Json.Linq;
using SimpleCore.Net;
using static SimpleCore.Diagnostics.LogCategories;
using MimeType = SimpleCore.Net.MimeType;

// ReSharper disable InconsistentNaming

// ReSharper disable UnusedMember.Global

namespace SmartImage.Lib.Utilities
{
	public static class ImageHelper
	{
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

		/*
		 * https://github.com/mikf/gallery-dl
		 * https://github.com/regosen/gallery_get
		 */


		public static DisplayResolutionType GetDisplayResolution(int w, int h)
		{
			/*
			 *	Other			W < 1280	
			 *	[HD, FHD)		[1280, 1920)	1280 <= W < 1920	W: >= 1280 < 1920
			 *	[FHD, QHD)		[1920, 2560)	1920 <= W < 2560	W: >= 1920 < 2560
			 *	[QHD, UHD)		[2560, 3840)	2560 <= W < 3840	W: >= 2560 < 3840
			 *	[UHD, ∞)											W: >= 3840
			 */

			return (w, h) switch
			{
				/*
				 * Specific resolutions
				 */

				(640, 360) => DisplayResolutionType.nHD,


				/*
				 * General resolutions
				 */

				_ => w switch
				{
					>= 1280 and < 1920 => DisplayResolutionType.HD,
					>= 1920 and < 2560 => DisplayResolutionType.FHD,
					>= 2560 and < 3840 => DisplayResolutionType.QHD,
					>= 3840            => DisplayResolutionType.UHD,
					_                  => DisplayResolutionType.Unknown,
				}
			};

		}

		/// <summary>
		/// Determines whether <paramref name="url"/> is a direct image link
		/// </summary>
		/// <remarks>A direct image link is a link which points to a binary image file</remarks>
		public static bool IsDirect(string url) => MediaTypes.IsDirect(url, MimeType.Image);


		public static bool IsDirect2(string url)
		{
			// todo


			/*
			 * https://github.com/PactInteractive/image-downloader
			 */

			return Regex.IsMatch(
				url,
				@"(?:([^:\/?#]+):)?(?:\/\/([^\/?#]*))?([^?#]*\.(?:bmp|gif|ico|jfif|jpe?g|png|svg|tiff?|webp))(?:\?([^#]*))?(?:#(.*))?",
				RegexOptions.IgnoreCase);
		}

		/// <summary>
		/// Scans for direct image links in <paramref name="url"/>
		/// </summary>
		public static async Task<string[]> FindDirectImagesAsync(string url)
		{
			var rg = new List<string>();

			//<img.*?src="(.*?)"
			//href\s*=\s*"(.+?)"
			//var src  = "<img.*?src=\"(.*?)\"";
			//var href = "href\\s*=\\s*\"(.+?)\"";

			string html;

			try {
				html = WebUtilities.GetString(url);
			}
			catch (Exception e) {
				Debug.WriteLine($"{e.Message}", C_ERROR);
				return null;
			}

			var matches = Regex.Matches(html, "<a\\s+(?:[^>]*?\\s+)?href=\"([^\"]*)\"");


			for (int i = 0; i < matches.Count; i++) {
				var match  = matches[i];
				var groups = match.Groups;

				for (int j = 0; j < groups.Count; j++) {
					var group = groups[j];

					foreach (Capture capture in group.Captures) {

						rg.Add(capture.Value);
					}
				}
			}


			var task = Task.Run(() =>
			{

				string[] results = rg.AsParallel()
				                     .Where(e => Network.IsUri(e, out var u) && IsDirect2(u == null ? e : u.ToString()))
				                     .ToArray();


				return results;
			});


			return await task;
		}
	}

	public enum DisplayResolutionType
	{
		Unknown,

		nHD,
		HD,
		FHD,
		QHD,
		UHD,
	}
}