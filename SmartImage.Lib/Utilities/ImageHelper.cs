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

		public static bool IsDirect(string value) => MediaTypes.IsDirect(value, MimeType.Image);

		public static string[] Scan(string s)
		{
			//<img.*?src="(.*?)"
			//href\s*=\s*"(.+?)"

			var html = Network.GetString(s);

			//var src  = "<img.*?src=\"(.*?)\"";
			//var href = "href\\s*=\\s*\"(.+?)\"";
			var href = "<a\\s+(?:[^>]*?\\s+)?href=\"([^\"]*)\"";
			//var m  = Regex.Matches(html, src);
			var m2 = Regex.Matches(html, href);

			//Debug.WriteLine($"{s} {m.Count} {m2.Count}");


			for (int index = 0; index < m2.Count; index++) {
				Match match = m2[index];
				var   v     = match.Groups;

				for (int i = 0; i < v.Count; i++) {
					Group @group = v[i];

					foreach (Capture capture in @group.Captures) {
						// this works but it's slow
						if (Network.IsUri(capture.Value, out var u)) {
							Debug.WriteLine($"[{index}, {i}] {u}");

						}
					}
				}
			}


			var rg = new List<string>();

			return rg.ToArray();
		}


		public static string ResolveDirectLink(string s)
		{
			//todo: WIP
			string d = "";

			try {
				var    uri  = new Uri(s);
				string host = uri.Host;


				var docp  = new HtmlParser();

				var html = Network.GetSimpleResponse(s);

				if (host.Contains("danbooru")) {
					Debug.WriteLine("danbooru");


					var jObject = JObject.Parse(html.Content);

					d = (string) jObject["file_url"]!;


					return d;
				}

				var doc=docp.ParseDocument(html.Content);

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