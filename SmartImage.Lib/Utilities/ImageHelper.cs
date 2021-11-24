using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using AngleSharp.Html.Dom;
using JetBrains.Annotations;
using Kantan.Net;
using Kantan.Utilities;
using Novus.Win32;
using RestSharp;
using static Kantan.Diagnostics.LogCategories;

#pragma warning disable CS0618
#pragma warning disable SYSLIB0014
#pragma warning disable CA1416
// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable CognitiveComplexity
// ReSharper disable PossibleNullReferenceException
// ReSharper disable UnusedParameter.Local
// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace SmartImage.Lib.Utilities;

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

	#region Cli

	public static Dictionary<string, string> UtilitiesMap
	{
		get
		{
			var rg = new Dictionary<string, string>();

			foreach (string exe in Utilities) {
				string path = FileSystem.SearchInPath(exe);

				rg.Add(exe, path);
			}

			return rg;

		}
	}

	public static readonly List<string> Utilities = new()
	{
		"ffmpeg.exe", "ffprobe.exe", "magick.exe", "youtube-dl.exe", "gallery-dl.exe"
	};

	#endregion

	private const int TimeoutMS = 1000;

	[CanBeNull]
	public static string Download(Uri src, string path)
	{
		var filename = UriUtilities.NormalizeFilename(src);

		string combine = Path.Combine(path, filename);

		using var wc = new WebClient();

		Debug.WriteLine($"{nameof(ImageHelper)}: Downloading {src} to {combine} ...", C_DEBUG);

		try {
			wc.DownloadFile(src.ToString(), combine);
			// WebUtilities.GetFile(src.ToString(), combine);
			// using var h = new HttpClient();
			// h.DownloadFile(src.ToString(), combine);


			return combine;
		}
		catch (Exception e) {
			Debug.WriteLine($"{nameof(ImageHelper)}: {e.Message}", C_ERROR);
			return null;
		}
	}


	/// <summary>
	/// Scans for direct images within a webpage.
	/// </summary>
	/// <param name="url">Url to search</param>
	/// <param name="count">Number of direct images to return</param>
	/// <param name="timeoutMS"></param>
	public static async Task<List<DirectImage>> ScanForImages(string url, int count = 10, long timeoutMS = TimeoutMS)
	{
		var images = new List<DirectImage>();

		IHtmlDocument document;

		try {
			document = WebUtilities.GetHtmlDocument(url);
		}
		catch (Exception e) {
			Debug.WriteLine($"{nameof(ImageHelper)}: {e.Message}", C_ERROR);

			return null;
		}

		using var cts  = new CancellationTokenSource();
		var       flat = new List<string>();

		flat.AddRange(document.QuerySelectorAttributes("a", "href"));
		flat.AddRange(document.QuerySelectorAttributes("img", "src"));

		flat = flat.Where(x=>x!=null).Distinct().ToList();

		var tasks = new List<Task<DirectImage>>();

		var hostComponent = UriUtilities.GetHostComponent(new Uri(url));

		switch (hostComponent) {
			case "www.deviantart.com":
				//https://images-wixmp-
				flat = flat.Where(x => x.StartsWith("https://images-wixmp")).ToList();
				break;
			default:
				break;
		}
		

		for (int i = 0; i < flat.Count; i++) {
			int iCopy = i;

			tasks.Add(Task<DirectImage>.Factory.StartNew(() =>
			{
				string s = flat[iCopy];

				if (IsImage(s, (int) timeoutMS, DirectImageCriterion.Binary, out var di)) {
					return di;
				}

				return null;

			}, cts.Token));
		}

		while (tasks.Any() && count != 0) {
			var task = await Task.WhenAny(tasks);
			tasks.Remove(task);

			var result = task.Result;

			if (result is { } && count > 0) {
				result.Url = new Uri(UriUtilities.NormalizeUrl(result.Url));
				images.Add(result);
				count--;
			}
		}


		return images;
	}

	public static bool IsImage(string url, out DirectImage di, DirectImageCriterion directCriterion = DirectImageCriterion.Binary)
		=> IsImage(url, TimeoutMS, directCriterion, out di);

	
	public static bool IsImage(string url, long timeout, DirectImageCriterion directCriterion, out DirectImage di)
	{
		di = new DirectImage(){};

		switch (directCriterion) {
			case DirectImageCriterion.Regex:
				var image = Regex.IsMatch(
					url,
					@"(?:([^:\/?#]+):)?(?:\/\/([^\/?#]*))?([^?#]*\.(?:bmp|gif|ico|jfif|jpe?g|png|svg|tiff?|webp))(?:\?([^#]*))?(?:#(.*))?",
					RegexOptions.IgnoreCase);
				di.Url = new Uri(url);
				
				return image;
			case DirectImageCriterion.Binary:
				if (!UriUtilities.IsUri(url, out var u)) {
					return false;
				}

				var response = HttpUtilities.GetResponse(u.ToString(), (int) timeout, Method.HEAD);

				if (!response.IsSuccessful) {

					return false;
				}

				di.Url = new Uri(url);

				di.Response = response;

				/* Check content-type */

				// The content-type returned from the response may not be the actual content-type, so
				// we'll resolve it using binary data instead to be sure
				bool a, b;

				try {
					var stream = WebUtilities.GetStream(url);
					var buffer = new byte[256];
					stream.Read(buffer, 0, buffer.Length);
					// var rg = response.RawBytes;
					var m = MediaTypes.ResolveFromData(buffer);
					a         = m.StartsWith("image") && m != "image/svg+xml";
					b         = response.ContentLength is -1 or >= 50_000;
					di.Stream = stream;
				}
				catch {
					a = response.ContentType.StartsWith("image") && response.ContentType != "image/svg+xml";
					b = response.ContentLength >= 50_000;
				}


				// var b = stream.Length >= 50_000;

				return a && b;
			default:
				throw new ArgumentOutOfRangeException(nameof(directCriterion), directCriterion, null);
		}

	}

	/*
	 * Direct images are URIs that point to a binary image file
	 */


	internal static string AsPercent(this float n)
	{
		/*
		 * Some engines may return the similarity value as 0f < n < 1f
		 * Therefore, similarity is normalized (100f * n) and stored as 0f < n < 100f
		 */

		return $"{n / 100:P}";
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

	public static Bitmap ResizeImage(Bitmap mg, Size newSize)
	{
		// todo
		/*
		 * Adapted from https://stackoverflow.com/questions/5243203/how-to-compress-jpg-image
		 */
		double ratio         = 0d;
		double myThumbWidth  = 0d;
		double myThumbHeight = 0d;
		int    x             = 0;
		int    y             = 0;

		Bitmap bp;

		if ((mg.Width / Convert.ToDouble(newSize.Width)) > (mg.Height / Convert.ToDouble(newSize.Height))) {
			ratio = Convert.ToDouble(mg.Width) / Convert.ToDouble(newSize.Width);
		}
		else {
			ratio = Convert.ToDouble(mg.Height) / Convert.ToDouble(newSize.Height);
		}

		myThumbHeight = Math.Ceiling(mg.Height / ratio);
		myThumbWidth  = Math.Ceiling(mg.Width / ratio);

		//Size thumbSize = new Size((int)myThumbWidth, (int)myThumbHeight);
		var thumbSize = new Size(newSize.Width, newSize.Height);
		bp = new Bitmap(newSize.Width, newSize.Height);
		x  = (newSize.Width - thumbSize.Width) / 2;
		y  = (newSize.Height - thumbSize.Height);
		// Had to add System.Drawing class in front of Graphics ---
		Graphics g = Graphics.FromImage(bp);
		g.SmoothingMode     = SmoothingMode.HighQuality;
		g.InterpolationMode = InterpolationMode.HighQualityBicubic;
		g.PixelOffsetMode   = PixelOffsetMode.HighQuality;
		var rect = new Rectangle(x, y, thumbSize.Width, thumbSize.Height);
		g.DrawImage(mg, rect, 0, 0, mg.Width, mg.Height, GraphicsUnit.Pixel);

		return bp;

	}
}

public enum DirectImageCriterion
{
	Binary,
	Regex
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