using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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

		/*
		 * https://github.com/mikf/gallery-dl
		 * https://github.com/regosen/gallery_get
		 */


		public static (int Width, int Height) GetResolution(string s)
		{
			using var bmp = Image.FromFile(s);

			return (bmp.Width, bmp.Height);
		}

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

		/// <summary>
		/// Scans for direct image links in <paramref name="url"/>
		/// </summary>
		public static string[] FindDirectImages(string url)
		{
			var rg = new List<string>();

			//<img.*?src="(.*?)"
			//href\s*=\s*"(.+?)"
			//var src  = "<img.*?src=\"(.*?)\"";
			//var href = "href\\s*=\\s*\"(.+?)\"";

			var html = WebUtilities.GetString(url);

			const string HREF_PATTERN = "<a\\s+(?:[^>]*?\\s+)?href=\"([^\"]*)\"";

			var m2 = Regex.Matches(html, HREF_PATTERN);


			for (int i = 0; i < m2.Count; i++) {
				var match  = m2[i];
				var groups = match.Groups;

				for (int j = 0; j < groups.Count; j++) {
					var group = groups[j];

					foreach (Capture capture in group.Captures) {

						rg.Add(capture.Value);
					}
				}
			}

			string[] results = null;


			var t = Task.Run(() =>
			{
				// todo: is running PLINQ within a task thread-safe?

				results = rg.AsParallel()
				            .Where(e => Network.IsUri(e, out _) && IsDirect(e))
				            .ToArray();

				Debug.WriteLine($"{nameof(FindDirectImages)}: {rg.Count} -> {results.Length}", C_DEBUG);
			});


			var timeout = TimeSpan.FromSeconds(3);

			if (t.Wait(timeout)) {
				//
			}
			else {
				Debug.WriteLine($"{nameof(FindDirectImages)}: timeout!", C_WARN);
			}


			return results;
		}


		/*public static string ResolveDirectLink(string s)
		{
			string d = "";

			try {
				var    uri  = new Uri(s);
				string host = uri.Host;


				var parser  = new HtmlParser();

				var html = Network.GetSimpleResponse(s);

				if (host.Contains("danbooru")) {
					Debug.WriteLine("danbooru");


					var jObject = JObject.Parse(html.Content);

					d = (string) jObject["file_url"]!;


					return d;
				}

				var doc=parser.ParseDocument(html.Content);

				string sel = "//img";

				var nodes = doc.Body.SelectNodes(sel);

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
		}*/
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