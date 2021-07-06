using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using SimpleCore.Net;
using SimpleCore.Utilities;

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


		/// <summary>
		/// Determines whether <paramref name="url"/> is a direct image link
		/// </summary>
		/// <remarks>A direct image link is a link which points to a binary image file</remarks>
		public static bool IsDirect(string url, DirectType directType = DirectType.Regex)
		{
			return directType switch
			{
				DirectType.Binary => MediaTypes.IsDirect(url, MimeType.Image),
				DirectType.Regex =>
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

		public static List<string> FindDirectImages(string url) => FindDirectImages(url, out _);

		public static List<string> FindDirectImages(string url, out List<Image> images) =>
			FindDirectImages(url, out images, DirectType.Regex, 5, 10, 1, true, null);

		/// <summary>
		/// Scans for direct images within a webpage.
		/// </summary>
		/// <param name="url">Url to search</param>
		/// <param name="images">List of images (applicable iff <paramref name="readImage"/> is <c>true</c>)</param>
		/// <param name="directType">Which criterion to use to determine whether a URI is a direct image </param>
		/// <param name="count">Number of direct images to return</param>
		/// <param name="fragmentSize">Size of the fragments which a respective task operates on</param>
		/// <param name="pingTimeSec"></param>
		/// <param name="readImage">Whether to read image metadata</param>
		/// <param name="imageFilter">Filter criteria for images (applicable iff <paramref name="readImage"/> is <c>true</c>)</param>
		public static List<string> FindDirectImages(string url, out List<Image> images, DirectType directType,
		                                            int count, int fragmentSize, double pingTimeSec,
		                                            bool readImage, Predicate<Image> imageFilter)
		{
			imageFilter ??= (x) => true;

			var pingTime = TimeSpan.FromSeconds(pingTimeSec);

			images = new List<Image>();

			var directImages = new List<string>();

			// Trace.WriteLine($"{url} | {directType} | {pingTime} | " +
			//                 $"{fragmentSize} | {readImage} | "      +
			//                 $"{imageFilter} | {count}");

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

			var fragments = flat.Chunk(fragmentSize).ToArray();

			var tasks = new List<Task>();

			count = Math.Clamp(count, count, flat.Count);

			for (int i = 0; i < fragments.Length; i++) {

				int iCopy = i;

				var imagesCopy = images;

				tasks.Add(Task.Factory.StartNew(() =>
				{

					foreach (string currentUrl in fragments[iCopy]) {
						if (directImages.Count >= count) {
							return;
						}

						if (Network.IsUri(currentUrl, out var uri)) {
							if (Network.IsUriAlive(uri, pingTime)) {

								bool direct = IsDirect(currentUrl, directType);

								if (direct) {
									bool isValid = !readImage;

									if (readImage) {
										var stream = WebUtilities.GetStream(currentUrl);

										if (stream.CanRead) {

											try {
												var img = Image.FromStream(stream);
												//isValid = true;

												//Debug.WriteLine($"{img.Width} {img.Height}");
												isValid = imageFilter(img);

												if (isValid) {
													imagesCopy.Add(img);
												}
											}
											catch (Exception) {
												isValid = false;
											}
										}
									}

									if (directImages.Count >= count) {
										return;
									}

									if (isValid) {
										directImages.Add(currentUrl);
										Debug.WriteLine($">>> {currentUrl}");

									}
								}


							}

						}
					}
				}, cts.Token));

			}

			Task.WaitAll(tasks.ToArray());

			return directImages;

		}
	}

	public enum DirectType
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