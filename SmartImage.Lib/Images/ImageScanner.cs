// Read S SmartImage.Lib BaseImageHost.cs
// 2023-07-08 @ 8:13 PM

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Immutable;
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
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Model;
using SmartImage.Lib.Utilities;
using SmartImage.Lib.Utilities.Integration;

// ReSharper disable InconsistentNaming

namespace SmartImage.Lib.Images;


public static partial class ImageScanner
{

	static ImageScanner()
	{
		s_logger = AppSupport.Factory.CreateLogger(nameof(ImageScanner));

		Client = (FlurlClient) FlurlHttp.Clients.GetOrAdd(nameof(ImageScanner), null, static builder =>
		{
			// builder.Settings.Redirects.ForwardAuthorizationHeader = true;
			// builder.Settings.Redirects.AllowSecureToInsecure      = true;

			builder.Settings.AllowedHttpStatusRange = "*";
			builder.Settings.HttpVersion            = "2.0";

			builder.Headers.AddOrReplace("User-Agent", HttpUtilities.UserAgent);

			// builder.AllowAnyHttpStatus();

			builder.WithAutoRedirect(true);

			builder.OnError(static f =>
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


	public const char URL_DELIM = '/';

	/*
	 * TODO:
	 *
	 * Aggregate
	 * Highest
	 *
	 * Gallery-DL
	 */

#region Regex

	[GeneratedRegex("""(?i)<(?:img|video|source)\s[^>]*src(?:set)?=[\"]?(?<URL>[^\"\s>]+)""", RegexOptions.Compiled, "en-US")]
	private static partial Regex r_imgSrc();

	[GeneratedRegex("""(?i)(?:[^?&#"'>\s]+)\.(?:jpe?g|jpe|png|gif|web[mp]|mp4|mkv|og[gmv]|opus)(?:[^"'<>\s]*)?""", RegexOptions.Compiled, "en-US")]
	private static partial Regex r_imgExt();

	[GeneratedRegex("""(?i)(?:<base\s.*?href=[\"]?)(?<url>[^\"' >]+)""", RegexOptions.Compiled, "en-US")]
	private static partial Regex r_imgHtml();

#endregion

#region Images

	public static readonly IImageFormat[] Formats = [PngFormat.Instance, JpegFormat.Instance, BmpFormat.Instance, GifFormat.Instance];

	public static readonly string[] Extensions = Formats.SelectMany(static fmt => fmt.FileExtensions).ToArray();

#endregion

	public static readonly string[] UrlPartBlacklists = ["thumbs", ".svg", ".ico", "twitter.svg", "pinterest.svg"];


	/// <summary>
	/// Scans for images within the webpage located at <paramref name="url"/>; if <paramref name="url"/> itself
	/// points to binary image data, it is returned.
	/// </summary>
	public static async Task<bool> ScanImagesAsync(Url url, ChannelWriter<UniImage> cw, CancellationToken ct = default)
	{
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


		await Task.WhenAll(urls.Select(async u => await Body(u, ct)));

		// await Parallel.ForEachAsync(urls, po, Body);

	ret:
		doc?.Dispose();
		cw.TryComplete();
		return true;

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
	}


	public static IEnumerable<string> GetImageUrls(string html, Url url, bool heuristicFilter = true)
	{
		var imgUrlsSrc = r_imgSrc().Matches(html).Select(static m => m.Groups["URL"].Value);
		var imgUrlsExt = r_imgExt().Matches(html).Select(static m => m.Value);
		var imgUrls    = imgUrlsSrc.Concat(imgUrlsExt);

		Match  baseMatch = r_imgHtml().Match(html);
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

		var cmd = Cli.Wrap(BaseOSIntegration.GALLERY_DL);

		cmd.WithArguments($"-G {cri}")
			.WithStandardOutputPipe(PipeTarget.ToStringBuilder(sbOut))
			.WithStandardOutputPipe(PipeTarget.ToStringBuilder(sbErr));

		var cr = await cmd.ExecuteAsync(ct);

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

	public static IImageHash ImageHasher { get; } = new PerceptualHash();

	public static readonly string[] LegalSchemes = ["http", "https"];

	public static Image ResizeByFactor(this ISImage image, Size newSize)
	{
		int origWidth  = image.Width;
		int origHeight = image.Height;

		double widthRatio  = (double) newSize.Width  / origWidth;
		double heightRatio = (double) newSize.Height / origHeight;
		double scale       = Math.Min(widthRatio, heightRatio);

		if (scale >= 1.0)
			return image.Clone();

		int newWidth  = (int) (origWidth  * scale);
		int newHeight = (int) (origHeight * scale);

		// Resize the image
		var resized = image.Clone(ctx => ctx.Resize(new ResizeOptions()
		{
			Size = new Size(newWidth, newHeight),

		}));
		return resized;
	}

	public static async ValueTask<IFlurlResponse> GetResponseAsync(Url value, CancellationToken ct)
	{
		// value = value.CleanString();
		/*if (value.Scheme == "javascript") {
			throw new ArgumentException($"{value}");
		}*/

		var req1 = await Client.Request(value)
			           .GetAsync(cancellationToken: ct);

		// var req  = ValueTask.FromResult(req1);

		// var res = await req.GetAsync(cancellationToken: ct);

		/*
		if (res.ResponseMessage.StatusCode == HttpStatusCode.NotFound) {
			throw new ArgumentException($"{value} returned {HttpStatusCode.NotFound}");

		}
		*/

		return req1;
	}

}