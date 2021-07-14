using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using JetBrains.Annotations;
using Novus.Win32;
using SimpleCore.Net;
using SimpleCore.Utilities;
using static SimpleCore.Diagnostics.LogCategories;

// ReSharper disable CognitiveComplexity
// ReSharper disable PossibleNullReferenceException

// ReSharper disable UnusedParameter.Local

// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable AssignNullToNotNullAttribute

// ReSharper disable LoopCanBeConvertedToQuery

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


		/*
		 * Direct images are URIs that point to a binary image file
		 */


		/// <summary>
		/// Determines whether <paramref name="url"/> is a direct image link
		/// </summary>
		/// <remarks>A direct image link is a link which points to a binary image file</remarks>
		public static bool IsDirect(string url, DirectImageType directType = DirectImageType.Regex)
		{
			return directType switch
			{
				DirectImageType.Binary => MediaTypes.IsDirect(url, MimeType.Image),
				DirectImageType.Regex =>
					/*
					 * https://github.com/PactInteractive/image-downloader
					 */
					Regex.IsMatch(
						url,
						@"(?:([^:\/?#]+):)?(?:\/\/([^\/?#]*))?([^?#]*\.(?:bmp|gif|ico|jfif|jpe?g|png|svg|tiff?|webp))(?:\?([^#]*))?(?:#(.*))?",
						RegexOptions.IgnoreCase),
				_ => throw new ArgumentOutOfRangeException(nameof(directType), directType, null)
			};

		}


		#region Cli

		private const string MAGICK_EXE     = "magick.exe";
		private const string GALLERY_DL_EXE = "gallery-dl.exe";
		private const string YOUTUBE_DL_EXE = "youtube-dl.exe";
		private const string FFPROBE_EXE    = "ffprobe.exe";
		private const string FFMPEG_EXE     = "ffmpeg.exe";

		public static Dictionary<string, string> UtilitiesMap
		{
			get
			{
				var rg = new Dictionary<string, string>();

				foreach (string exe in Utilities) {
					var path = FileSystem.SearchInPath(exe);


					rg.Add(exe, path);
				}

				return rg;

			}
		}

		public static readonly List<string> Utilities = new()
		{
			FFMPEG_EXE, FFPROBE_EXE, MAGICK_EXE, YOUTUBE_DL_EXE, GALLERY_DL_EXE
		};

		#endregion


		public static Image GetImage(string s)
		{
			// TODO: Using Image objects creates a memory leak; disposing them doesn't fix anything either (?)

			using var wc = new WebClient();

			byte[] buf = wc.DownloadData(s);

			var stream = new MemoryStream(buf);

			Debug.WriteLine($"Alloc {buf.Length}");

			var image = Image.FromStream(stream);

			return image;
		}


		/// <summary>
		/// Scans for direct images within a webpage.
		/// </summary>
		/// <param name="url">Url to search</param>
		/// <param name="directType">Which criterion to use to determine whether a URI is a direct image </param>
		/// <param name="count">Number of direct images to return</param>
		/// <param name="pingTimeSec"></param>
		/// <param name="readImage">Whether to read image metadata</param>
		/// <param name="imageFilter">Filter criteria for images (applicable iff <paramref name="readImage"/> is <c>true</c>)</param>
		public static List<DirectImage> FindDirectImages(string url, DirectImageType directType = DirectImageType.Regex,
		                                                 int count = 5, double pingTimeSec = 1, bool readImage = true,
		                                                 Predicate<Image> imageFilter = null)
		{
			var images = new List<DirectImage>();

			string gallerydl = UtilitiesMap[GALLERY_DL_EXE];

			if (gallerydl != null) {

				//Trace.WriteLine($"Using gallery-dl!");

				var output = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						FileName               = gallerydl,
						Arguments              = $"-G {url}",
						RedirectStandardOutput = true,
						RedirectStandardError  = true,
						CreateNoWindow         = true
					}
				};

				output.Start();

				var standardOutput = output.StandardOutput;

				while (!standardOutput.EndOfStream) {
					string str = standardOutput.ReadLine()
					                           .Split('|')
					                           .First();

					var di = new DirectImage
					{
						Direct = new Uri(str),
					};

					if (readImage) {
						di.Image = GetImage(str);
					}

					images.Add(di);
				}

				var standardError = output.StandardError;

				while (!standardError.EndOfStream) {
					string line = standardError.ReadLine();

					if (line != null) {
						goto manual;
					}
				}


				goto ret;
			}

			manual:

			imageFilter ??= (x) => true;

			var pingTime = TimeSpan.FromSeconds(pingTimeSec);

			IHtmlDocument document;

			try {
				string html   = WebUtilities.GetString(url);
				var    parser = new HtmlParser();

				document = parser.ParseDocument(html);

			}
			catch (Exception e) {
				Debug.WriteLine($"{e.Message}");

				return null;
			}

			var cts = new CancellationTokenSource();

			var flat = new List<string>();

			flat.AddRange(document.QuerySelectorAttributes("a", "href"));
			flat.AddRange(document.QuerySelectorAttributes("img", "src"));

			flat = flat.Distinct().ToList();

			var options = new ParallelOptions
			{
				MaxDegreeOfParallelism = Int32.MaxValue,
				TaskScheduler          = TaskScheduler.Default,
				CancellationToken      = cts.Token
			};

			var imagesCopy = images;

			Parallel.For(0, flat.Count, options, i =>
			{
				string currentUrl = flat[i];

				if (imagesCopy.Count >= count) {
					return;
				}

				if (!Network.IsUri(currentUrl, out var uri))
					return;

				if (!Network.IsAlive(uri, (long) pingTime.TotalMilliseconds)) {
					Debug.WriteLine($"{uri} isn't alive");
					return;
				}

				if (!IsDirect(currentUrl, directType))
					return;

				var di = new DirectImage
				{
					Direct = new Uri(currentUrl)
				};


				bool isValid = !readImage;

				if (readImage) {
					try {
						var img = GetImage(currentUrl);

						isValid = imageFilter(img);

						if (isValid) {
							di.Image = img;

						}
						else {
							img.Dispose();
						}
					}
					catch (Exception) {
						isValid = false;
					}
				}

				if (imagesCopy.Count >= count) {
					return;
				}

				if (isValid) {
					imagesCopy.Add(di);
				}
			});


			/*
			 * Tasks
			 * 1		5.19
			 * 2		4.68
			 * 3		4.54
			 * 4		4.42
			 *
			 * Parallel
			 * 1		4.59
			 * 2		4.37
			 * 3		4.28
			 * 4		4.34
			 * 5		4.38
			 * 6		4.55
			 * 7		4.36
			 * 8		4.45
			 *
			 * 9		3.84
			 * 10		3.56
			 * 11		3.45
			 * 12		3.52
			 * 13		3.63
			 * 14		3.52
			 *
			 * 15		3.39
			 * 16		3.52
			 * 17		3.58
			 * 18		3.47
			 * 19		3.33
			 *
			 * 20		3.13
			 * 21		2.89
			 * 22		2.87
			 * 23		2.89
			 */

			images = imagesCopy;

			ret:

			if (readImage) {
				images = images.OrderByDescending(x => x.Image.Width * x.Image.Height).ToList();
			}

			return images;

		}

		internal static string AsPercent(this float n)
		{
			/*
			 * Some engines may return the similarity value as 0f < n < 1f
			 * Therefore, similarity is normalized (100f * n) and stored as 0f < n < 100f
			 */

			return $"{n / 100:P}";
		}
	}

	public struct DirectImage : IDisposable
	{
		public Uri Direct { get; internal set; }

		[CanBeNull]
		public Image Image { get; internal set; }

		public override string ToString()
		{
			return $"{Direct} {Image?.Width}x{Image?.Height}";
		}

		public void Dispose()
		{
			Image?.Dispose();
		}
	}

	public enum DirectImageType
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
}