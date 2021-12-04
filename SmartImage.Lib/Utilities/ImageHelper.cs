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
using JetBrains.Annotations;
using Kantan.Net;
using Novus.Win32;
using static Kantan.Diagnostics.LogCategories;
#pragma warning disable CS0168

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


	/// <summary>
	///     Scans for direct images within a webpage.
	/// </summary>
	/// <param name="url">Url to search</param>
	/// <param name="count">Number of direct images to return</param>
	/// <param name="timeoutMS"></param>
	public static async Task<List<DirectImage>> ScanForImages(string url, int count = 10, long timeoutMS = TimeoutMS)
	{
		var images = new List<DirectImage>();

		IHtmlDocument document = null;

		try {
			var client = new HttpClient();
			var task   = client.GetStringAsync(url);
			task.Wait();
			string result = task.Result;

			var parser = new HtmlParser();
			document = parser.ParseDocument(result);
			client.Dispose();

			// document = WebUtilities.GetHtmlDocument(url);
		}
		catch (Exception e) {
			// Debug.WriteLine($"{nameof(WebUtilities)}: {e.Message}", C_ERROR);
			document?.Dispose();
			return images;
		}

		using var cts = new CancellationTokenSource();

		var urls = new List<string>();

		urls.AddRange(document.QuerySelectorAttributes("a", "href"));
		urls.AddRange(document.QuerySelectorAttributes("img", "src"));

		urls = urls.Where(x => x != null).Select(u1 =>
		{

			if (UriUtilities.IsUri(u1, out Uri u2)) {
				return UriUtilities.NormalizeUrl(u2);
			}

			return u1;
		}).Distinct().ToList();


		var tasks = new List<Task<DirectImage>>();

		string hostComponent = UriUtilities.GetHostComponent(new Uri(url));

		switch (hostComponent) {
			case "www.deviantart.com":
				//https://images-wixmp-
				urls = urls.Where(x => x.StartsWith("https://images-wixmp")).ToList();
				break;
			case "twitter.com":
				urls = urls.Where(x => !x.Contains("profile_banners"))
				           .ToList();
				break;
		}


		for (int i = 0; i < urls.Count; i++) {
			int iCopy = i;

			tasks.Add(Task<DirectImage>.Factory.StartNew(() =>
			{
				string s = urls[iCopy];

				if (IsImage(s, (int) timeoutMS, out var di)) {
					return di;
				}

				di?.Dispose();

				return null;

			}, cts.Token));
		}

		while (tasks.Any() && count != 0) {
			var task = await Task.WhenAny(tasks);
			tasks.Remove(task);

			DirectImage result = task.Result;

			if (result is { } && count > 0) {
				// result.Url = new Uri(UriUtilities.NormalizeUrl(result.Url));
				images.Add(result);
				count--;
			}
			else {
				result?.Dispose();
			}
		}

		document.Dispose();

		return images;
	}

	public static bool IsImage(string url, out DirectImage di) => IsImage(url, TimeoutMS, out di);

	public static bool IsImage(string url, long timeout, out DirectImage di)
	{
		di = new DirectImage();

		if (!UriUtilities.IsUri(url, out var u)) {
			return false;
		}


		// var response = HttpUtilities.GetResponse(url, (int) timeout, Method.HEAD);
		var response = HttpUtilities.GetHttpResponse(url, (int) timeout, HttpMethod.Head);


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
		bool type, size;

		const string svg_xml    = "image/svg+xml";
		const string image      = "image";
		const int    min_size_b = 50_000;

		var length = response.Content.Headers.ContentLength;
		di.Response = response;

		try {
			using var client = new HttpClient();
			var       task   = client.GetStreamAsync(url);
			task.Wait((int) timeout);

			var stream = task.Result;

			var buffer = new byte[256];
			stream.Read(buffer, 0, buffer.Length);
			var m = MediaTypes.ResolveFromData(buffer);
			type = m.StartsWith(image) && m != svg_xml;
			size = length is -1 or >= min_size_b;
			stream.Dispose();
		}
		catch (Exception x) {
			var value = response.Content.Headers.ContentType;
			type = value.MediaType.StartsWith(image) && value.MediaType != svg_xml;
			size = length >= min_size_b;
			// Debug.WriteLine($"{nameof(IsImage)}: {x.Message}");
		}

		return type && size;
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
				_                  => DisplayResolutionType.Unknown
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