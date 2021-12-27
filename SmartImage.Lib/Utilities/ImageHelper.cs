using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Flurl.Http;
using JetBrains.Annotations;
using Kantan.Net;
using Novus.OS;
using static Kantan.Diagnostics.LogCategories;
using HttpRequestMessage = System.Net.Http.HttpRequestMessage;

#pragma warning disable CS0168
#pragma warning disable IDE0059

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
	private const int TimeoutMS = 1000;


	/// <summary>
	///     Scans for direct images within a webpage.
	/// </summary>
	/// <param name="url">Url to search</param>
	/// <param name="count">Number of direct images to return</param>
	/// <param name="timeoutMS"></param>
	/// <param name="token"></param>
	public static List<DirectImage> ScanForImages(string url, int count = 10, long timeoutMS = TimeoutMS,
	                                              CancellationToken? token = null)
	{
		var images = new List<DirectImage>();

		IHtmlDocument document = null;

		var c = token ?? CancellationToken.None;

		try {
			var client = new HttpClient();
			var task   = client.GetStringAsync(url);
			task.Wait();
			string result = task.Result;

			var parser = new HtmlParser();
			document = parser.ParseDocument(result);
			client.Dispose();
		}
		catch (Exception e) {
			goto _Return;
		}


		var urls = new List<string>();

		urls.AddRange(document.QuerySelectorAttributes("a", "href"));
		urls.AddRange(document.QuerySelectorAttributes("img", "src"));

		/*
		 * Normalize urls
		 */

		urls = urls.Where(url => url != null).Select(u =>
		{
			if (UriUtilities.IsUri(u, out Uri u2)) {
				return UriUtilities.NormalizeUrl(u2);
			}

			return u;
		}).Distinct().ToList();

		/*
		 * Filter urls if the host is known
		 */

		string hostComponent = UriUtilities.GetHostComponent(new Uri(url));

		switch (hostComponent) {
			case "www.deviantart.com":
				//https://images-wixmp-
				urls = urls.Where(x => x.Contains("images-wixmp"))
				           .ToList();
				break;
			case "twitter.com":
				urls = urls.Where(x => !x.Contains("profile_banners"))
				           .ToList();
				break;
		}


		var pr = Parallel.For(0, urls.Count, (i, pls) =>
		{
			string s = urls[i];

			if (IsImage(s, out var di, (int) timeoutMS, c)) {
				if (di is { } && count > 0) {
					images.Add(di);
					count--;
					pls.Break();

				}
				else {
					di?.Dispose();
				}
			}
			else {
				di?.Dispose();
			}
		});

		// Debug.WriteLine($"{nameof(ScanForImages)}: {pr}");

		_Return:
		document?.Dispose();
		return images;
	}


	public static bool IsImage(string url, out DirectImage di, int timeout = TimeoutMS, CancellationToken? token = null)
	{
		const string svg_xml    = "image/svg+xml";
		const string image      = "image";
		const int    min_size_b = 50_000;


		di = new DirectImage();

		if (!UriUtilities.IsUri(url, out Uri u)) {
			return false;
		}


		using var client = new HttpClient(){};

		var result=HttpUtilities.GetHttpResponse(url,timeout, HttpMethod.Get, token: token);
		// result.Wait();
		// var response = result.Result;
		var response = result;

		if (response is not { }) {
			return false;
		}

		if (!response.IsSuccessStatusCode) {
			response.Dispose();
			return false;
		}

		di.Url      = new Uri(url);
		di.Response = response;

		/* Check content-type */

		// The content-type returned from the response may not be the actual content-type, so
		// we'll resolve it using binary data instead to be sure





		var length = response.Content.Headers.ContentLength;
		di.Response = response;

		string mediaType;

		try {
			// using var client = new HttpClient();

			// var task1 = client.GetStreamAsync(url, token ?? CancellationToken.None /*cts.Token*/);
			// var cts = new CancellationTokenSource((int) timeout);
			// cts.CancelAfter((int) timeout);
			// client.Timeout = TimeSpan.FromMilliseconds(timeout);
			// task.Wait(timeout);
			// task1.Wait();

			// var stream = task1.Result;
			// var buffer = new byte[256];
			// stream.Read(buffer, 0, buffer.Length);
			// stream.Flush();
			// stream.Dispose();
			var task = response.Content.ReadAsByteArrayAsync();
			task.Wait();

			mediaType = MediaTypes.ResolveFromData(task.Result);
		}
		catch (Exception x) {
			mediaType = response.Content.Headers.ContentType.MediaType;
		}

		// string mediaType = response.Content.Headers.ContentType.MediaType;


		bool type = mediaType.StartsWith(image) && mediaType != svg_xml;
		bool size = length is -1 or >= min_size_b;


		return type && size;
	}

	/*
	 * Direct images are URIs that point to a binary image file
	 */

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

	[CanBeNull]
	public static string Download(Uri src, string path)
	{
		string    filename = UriUtilities.NormalizeFilename(src);
		string    combine  = Path.Combine(path, filename);
		using var wc       = new WebClient();

		Debug.WriteLine($"{nameof(ImageHelper)}: Downloading {src} to {combine} ...", C_DEBUG);

		try {
			wc.DownloadFile(src.ToString(), combine);
			return combine;
		}
		catch (Exception e) {
			Debug.WriteLine($"{nameof(ImageHelper)}: {e.Message}", C_ERROR);
			return null;
		}
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

		if (mg.Width / Convert.ToDouble(newSize.Width) > mg.Height / Convert.ToDouble(newSize.Height)) {
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
		y  = newSize.Height - thumbSize.Height;
		// Had to add System.Drawing class in front of Graphics ---
		Graphics g = Graphics.FromImage(bp);
		g.SmoothingMode     = SmoothingMode.HighQuality;
		g.InterpolationMode = InterpolationMode.HighQualityBicubic;
		g.PixelOffsetMode   = PixelOffsetMode.HighQuality;
		var rect = new Rectangle(x, y, thumbSize.Width, thumbSize.Height);
		g.DrawImage(mg, rect, 0, 0, mg.Width, mg.Height, GraphicsUnit.Pixel);

		return bp;

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
				_                  => DisplayResolutionType.Unknown
			}
		};

	}

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
}

public enum DisplayResolutionType
{
	Unknown,

	nHD,
	HD,
	FHD,
	QHD,
	UHD
}