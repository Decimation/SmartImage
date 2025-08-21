// Read S SmartImage.Lib BaseImageHost.cs
// 2023-07-08 @ 8:13 PM

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Web;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Io;
using CliWrap;
using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;
using FlareSolverrSharp;
using Flurl.Http;
using Kantan.Net.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Novus.FileTypes;
using Novus.FileTypes.Uni;
using Novus.OS;
using Novus.Streams;
using Novus.Utilities;
using Novus.Win32.Structures.Other;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Model;
using SmartImage.Lib.Utilities;
using SmartImage.Lib.Utilities.Diagnostics;
using SmartImage.Lib.Utilities.Integration;

// ReSharper disable InconsistentNaming

namespace SmartImage.Lib.Images;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

public static class ImageScanner
{

	static ImageScanner()
	{
		s_logger = AppSupport.Factory.CreateLogger(nameof(ImageScanner));

		Client = (FlurlClient) FlurlHttp.Clients.GetOrAdd(nameof(ImageScanner), null, builder =>
		{
			// builder.Settings.Redirects.ForwardAuthorizationHeader = true;
			// builder.Settings.Redirects.AllowSecureToInsecure      = true;

			builder.Settings.AllowedHttpStatusRange = "*";

			builder.Settings.HttpVersion = "2.0";

			builder.Headers.AddOrReplace("User-Agent", HttpUtilities.UserAgent);

			// builder.AllowAnyHttpStatus();

			builder.WithAutoRedirect(true);

			builder.OnError(f =>
			{
				s_logger.LogError(f.Exception, "{Call}", f);
				f.ExceptionHandled = true;

				// Debugger.Break();
				// f.ExceptionHandled = true;
				return;
			});
		});

	}


	public static FlurlClient Client { get; }

	private static readonly ILogger s_logger;

	/*
	 * TODO: DefaultCookiesProvider, and FlareSolverr
	 */


	private const char URL_DELIM = '/';

	/*
	 * TODO:
	 *
	 * Aggregate
	 * Highest
	 *
	 * Gallery-DL
	 */

	#region Regex

	private static readonly Regex r_imgSource = new(
		"""(?i)<(?:img|video|source)\s[^>]*src(?:set)?=[\"]?(?<URL>[^\"\s>]+)""",
		RegexOptions.Compiled
	);

	private static readonly Regex r_imgExt = new(
		"""(?i)(?:[^?&#"'>\s]+)\.(?:jpe?g|jpe|png|gif|web[mp]|mp4|mkv|og[gmv]|opus)(?:[^"'<>\s]*)?""",
		RegexOptions.Compiled
	);

	private static readonly Regex r_imgHtml = new(
		"""(?i)(?:<base\s.*?href=[\"]?)(?<url>[^\"' >]+)""",
		RegexOptions.Compiled
	);

	#endregion

	public static readonly string[] Extensions = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif"];

	/*[MURV]
	public static Stream ToStream(this Image image, IImageFormat format = null)
	{
		// TODO
		var ms = new MemoryStream();

		// If format is not specified, use the image's decoded format if available
		format ??= image.Metadata.DecodedImageFormat;
		image.Save(ms, format);
		ms.Rewind();

		return ms;
	}*/

	/*[MURV]
	public static byte[] ToBytes(this Image image, IImageFormat format = null)
	{
		using var ms = (MemoryStream) image.ToStream(format);

		return ms.ToArray();
	}*/

	/// <summary>
	/// Scans for images within the webpage located at <paramref name="url"/>; if <paramref name="url"/> itself
	/// points to binary image data, it is returned.
	/// </summary>
	public static async Task<bool> ScanImagesAsync(Url url, ChannelWriter<UniImage> cw, CancellationToken ct = default)
	{
		Stream stream;
		string sz = null;

		IHtmlDocument doc = null;

		/* Immediate search  */
		var uf = await UniImage.TryCreateAsync(url, autoInit: true, autoDisposeOnError: false, ct: ct);

		IFlurlResponse res;

		IFlurlRequest req;

		if (uf != UniImage.Null && uf.HasImageFormat) {
			await cw.WriteAsync(uf, ct);

			goto ret;
		}
		else {
			// uf.Stream.TrySeek();
			uf?.Dispose();

			req = Client.Request(url);
			res = await req.GetAsync(cancellationToken: ct);

			// stream = await res.GetStreamAsync();
			sz = await res.GetStringAsync();
		}


		/*if (!stream.CanRead) {
			stream.Dispose();
			goto ret;
		}*/

		var dp = new HtmlParser();

		doc = await dp.ParseDocumentAsync(sz);

		var urls = GetImageUrls(sz, url);

		var po = new ParallelOptions()
		{
			CancellationToken = ct,

		};


		async ValueTask Body(string s, CancellationToken token)
		{
			var uni = await UniImage.TryCreateAsync(s, autoInit: true, autoDisposeOnError: true, ct: token);

			if (token.IsCancellationRequested) {
				return;
			}

			if (uni != UniImage.Null && uni.HasImageFormat) {
				s_logger.LogTrace("{Name} {Uni}", nameof(ScanImagesAsync), uni);

				// await cw.WriteAsync(uni, token);
				await cw.WaitToWriteAsync(token);
				await cw.WriteAsync(uni, token);

			}
			else {
				uni?.Dispose();
			}
		}

		await Task.WhenAll(urls.Select(async u => await Body(u, ct)));

		// await Parallel.ForEachAsync(urls, po, Body);

	ret:
		doc?.Dispose();
		cw.TryComplete();
		return true;
	}


	public static IEnumerable<string> GetImageUrls(string html, Url url, bool heuristicFilter = true)
	{
		var imgUrlsSrc = r_imgSource.Matches(html).Select(static m => m.Groups["URL"].Value);
		var imgUrlsExt = r_imgExt.Matches(html).Select(static m => m.Value);
		var imgUrls    = imgUrlsSrc.Concat(imgUrlsExt);

		Match  baseMatch = r_imgHtml.Match(html);
		string baseUrl;

		if (baseMatch.Success) {
			baseUrl = baseMatch.Groups["url"].Value.TrimEnd(URL_DELIM);
		}
		else {
			if (url.ToString().EndsWith(URL_DELIM)) {
				baseUrl = url.ToString().TrimEnd(URL_DELIM);
			}
			else {
				baseUrl = Url.Parse(url); //todo

				// or Path.GetDirectoryName?
			}
		}

		var abs = imgUrls.Select(u =>
		{
			if (u.StartsWith("http"))
				return u;

			if (u.StartsWith("//"))

				// return url.Scheme + u.TrimStart(URL_DELIM);
				return Url.Combine(url.Scheme, u.TrimStart(URL_DELIM));

			if (u.StartsWith(URL_DELIM))

				// return url.Root + u;

				return Url.Combine(url.Root, u);


			// return baseUrl + URL_DELIM + u;
			return Url.Combine(baseUrl, URL_DELIM.ToString(), u);
		}).Select(u => Url.Decode(u, true)).Where(Url.IsValid).Distinct();

		if (heuristicFilter) {
			abs = abs.Where(u => !UrlPartBlacklists.Any(u.Contains));
		}

		return abs;
	}

	public static readonly string[] UrlPartBlacklists = ["thumbs", ".svg", ".ico", "twitter.svg", "pinterest.svg"];

	public static IEnumerable<string> GetImageUrls(IHtmlDocument doc)
	{
		// var a = doc.QueryAllAttribute("a", "href");
		// var b = doc.QueryAllAttribute("img", "src");

		var a = doc.Links.Select(static x => x.GetAttribute("href"));
		var b = doc.Images.Select(static x => x.Source);
		var c = a.Union(b);

		c = c.Distinct();

		return c;
	}

	public static async Task<UniImage[]> RunGalleryDLAsync(Url cri, CancellationToken ct = default)
	{
		// TODO: TEST
		// TODO: USE CHANNELS

		if (!BaseOSIntegration.Integration.IsGalleryDLInstalled) {
			return null;
		}

		var sbOut = new StringBuilder();
		var sbErr = new StringBuilder();

		var cmd = CliWrap.Cli.Wrap(BaseOSIntegration.GALLERY_DL);

		cmd.WithArguments($"-G {cri}")
			.WithStandardOutputPipe(PipeTarget.ToStringBuilder(sbOut))
			.WithStandardOutputPipe(PipeTarget.ToStringBuilder(sbErr));

		var cr = await cmd.ExecuteAsync();

		if (!cr.IsSuccess) {
			return null;
		}

		var s2 = sbErr.ToString().Split(Environment.NewLine);
		var rg = new ConcurrentBag<UniImage>();

		await Parallel.ForEachAsync(s2, ct, async (s1, token) =>
		{
			var uni = await UniImage.TryCreateAsync(s1, ct: token);

			if (uni != null) {
				rg.Add(uni);
			}


			token.ThrowIfCancellationRequested();
		});

		// p.Dispose();

		return rg.ToArray();
	}

	public class UniSimilarity
	{

		public UniImage Image { get; }

		public double Similarity { get; }

		public UniSimilarity(UniImage image, double similarity)
		{
			Image      = image;
			Similarity = similarity;
		}

	}

	/*public static async Task<IEnumerable<Item2>> Highest(SearchQuery query,
	                                                     IEnumerable<SearchResultItem> results,
	                                                     CancellationToken ct = default)
	{
		var plr = new ParallelOptions()
		{
			CancellationToken      = ct,
			MaxDegreeOfParallelism = -1,
		};

		/*await Parallel.ForEachAsync(results, plr, async (item, token) =>
		{
			var r = await ImageScanner.GetImageUrlsAsync(item.Url, token: token);
			Debug.WriteLine($"{item.Url} -> {r}");

		});#1#

		// results = results.Where(r => !r.IsRaw && r.Url != null);

		var cb = new ConcurrentBag<Item2>();

		foreach (var result in results) {

			// IDocument dd = await GetDocument2(u, ct);

			// var urls = await ImageScanner.GetImageUrlsAsync(result.Url, token: ct);

			IFlurlResponse response;

			try {
				response = await Client.Request(result.Url)
					           .OnError(call =>
					           {
						           call.ExceptionHandled = true;
					           })
					           .WithHeaders(new
					           {
						           // todo
						           User_Agent = R1.UserAgent1,
					           })
					           .WithTimeout(TimeSpan.FromSeconds(3.5))
					           .GetAsync(cancellationToken: ct);
			}
			catch (Exception e) {
				Debug.WriteLine($"{e.Message}");
				response = null;
			}

			if (response == null) {
				continue;
			}

			var stream = await response.GetStringAsync();

			// var parser = new HtmlParser();
			// var doc    = await parser.ParseDocumentAsync(stream);

			// var urls1 = ImageScanner.GetImageUrls(doc);
			var urls = GetImageUrls(stream, result.Url).ToArray();

			// doc.Dispose();
			// response.Dispose();

			Debug.WriteLine($"{result.Url} -> {urls.Length}");

			async ValueTask Body(string s, CancellationToken token)
			{
				IFlurlResponse resp;

				try {
					resp = await Client.Request(s)
						       .OnError(call =>
						       {
							       // call.ExceptionHandled = false;
						       })
						       .WithHeaders(new
						       {
							       // todo
							       User_Agent = R1.UserAgent1,
						       })
						       .WithTimeout(TimeSpan.FromSeconds(7.5))
						       .GetAsync(cancellationToken: ct);
				}
				catch (Exception e) {
					Debug.WriteLine($"{e.Message}");
					resp = null;
				}

				if (resp == null) {
					return;
				}

				var bin = await resp.GetStreamAsync();

				if (bin is { CanRead: true }) {
					bin.TrySeek();

					try {
						var img = await ISImage.DetectFormatAsync(bin, token);
						cb.Add(new Item2() { Image = img, Item = result });
						bin.TrySeek();

						// Debug.WriteLine($"{img}");
					}
					catch (Exception e) {
						Debug.WriteLine(e);
					}
				}

				resp.Dispose();

				// bin.Dispose();

			}

			await Parallel.ForEachAsync(urls, plr, Body);


		}

		return cb;
	}

	public static async Task<IEnumerable<SearchResultItem>> Aggregate(IEnumerable<SearchResultItem> results)
	{
		var groups = results.GroupBy(g => new { g.Artist });

		foreach (var v in groups) {
			Console.WriteLine($"{v.Key}");
		}

		return ( []);
	}*/

	public static IImageHash ImageHasher { get; } = new PerceptualHash();

}