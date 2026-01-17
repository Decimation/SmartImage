// Read S SmartImage.Lib BaseImageHost.cs
// 2023-07-08 @ 8:13 PM

using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Io;
using Argon;
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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Web;

// ReSharper disable SuggestVarOrType_Elsewhere

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

			builder.Headers.AddOrReplace("User-Agent", R1.UserAgent1);

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

	/*
	 * TODO: DefaultCookiesProvider, and FlareSolverr
	 */


	private static readonly ILogger s_logger;

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

	public static readonly IImageFormat[] ImageFormats = [PngFormat.Instance, JpegFormat.Instance, BmpFormat.Instance, GifFormat.Instance];

	public static readonly string[] FormatExtensions = ImageFormats.SelectMany(static fmt => fmt.FileExtensions).ToArray();

#endregion

#region Scanning/parsing

	public static readonly string[] UrlSegmentBlacklist = ["thumbs", ".svg", ".ico", "twitter.svg", "pinterest.svg", "favicon"];

	public static readonly string[] LegalSchemeWhitelist = ["http", "https"];

	internal const char URL_DELIM = '/';


	/*public static async Task<bool> ScanForImagesAsync(Url url, ChannelWriter<UniImage> cw, CancellationToken ct = default)
	{
		string sz = null;

		IHtmlDocument doc = null;

		/* Immediate search  #1#
		var uf = await UniImage.TryCreateAsync(url, autoInit: true, autoDisposeOnError: false, ct: ct);

		IFlurlResponse res;

		IFlurlRequest req;

		if (uf != UniImage.Null && uf.HasImageFormat) {
			await cw.WriteAsync(uf, ct);

			goto ret;
		}
		else {
			// uf.Stream.TrySeek();
			// ReSharper disable once MethodHasAsyncOverload
			uf?.Dispose();

			req = Client.Request(url);
			res = await req.GetAsync(cancellationToken: ct);

			// stream = await res.GetStreamAsync();
			sz = await res.GetStringAsync();
		}


		/*if (!stream.CanRead) {
			stream.Dispose();
			goto ret;
		}#1#

		var dp = new HtmlParser();

		doc = await dp.ParseDocumentAsync(sz);

		var urls = ParseImageUrlsByRegex(sz, url);


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
				s_logger.LogTrace("Scan: {Uni}", uni);

				// await cw.WriteAsync(uni, token);
				await cw.WaitToWriteAsync(token);
				await cw.WriteAsync(uni, token);

			}
			else {
				uni?.Dispose();
			}
		}
	}*/

	public static IEnumerable<string> ParseImageUrlsByRegex(string html, Url url, bool heuristicFilter = true)
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

		imgUrls = imgUrls.Distinct().Where(e =>
		{
			if (e.StartsWith("url(")) {
				return false;
			}

			return Url.IsValid(e);
		});

		if (heuristicFilter) {
			imgUrls = imgUrls.Where(static u => !UrlSegmentBlacklist.Any(u.Contains));
		}

		var abs = imgUrls.Select(u =>
		{
			if (u.StartsWith("http"))
				return u;

			if (u.StartsWith("//"))
				return Url.Combine(url.Scheme, u.TrimStart(URL_DELIM));

			if (u.StartsWith(URL_DELIM))
				return Url.Combine(url.Root, u);


			// return baseUrl + URL_DELIM + u;
			return Url.Combine(baseUrl, URL_DELIM.ToString(), u);
		});

		return abs;
	}

	public static IEnumerable<string> ParseImageUrlsByDoc(IHtmlDocument doc)
	{
		// var a = doc.QueryAllAttribute("a", "href");
		// var b = doc.QueryAllAttribute("img", "src");

		var a = doc.Links.Select(static x => x.GetAttribute("href"));
		var b = doc.Images.Select(static x => x.Source);
		var c = a.Union(b);

		c = c.Distinct();

		return c;
	}

	public static async ValueTask<IFlurlResponse> GetResponseAsync(Url value, CancellationToken ct)
	{
		var request = Client.Request(value);

		if (value.ToString().Contains("zerochan")) {
			// request = request.WithHeader("User-Agent", R1.Name);

			request = new FlurlRequest(value) { };
		}

		var response = await request.OnError(act => { act.ExceptionHandled = true; })
			               .GetAsync(cancellationToken: ct);

		return response;
	}

#endregion


	[Obsolete]
	public static async Task<UniImage[]> RunGalleryDLAsync(Url cri, CancellationToken ct = default)
	{
		// TODO: TEST
		// TODO: USE CHANNELS

		if (!BaseOSIntegration.Integration.IsGalleryDLInstalled) {
			return null;
		}

		var rg = new ConcurrentBag<UniImage>();

		var sbErr = new StringBuilder();

		var cmd = Cli.Wrap(BaseOSIntegration.GALLERY_DL);

		cmd.WithArguments([$"-G", cri])
			.WithStandardOutputPipe(PipeTarget.Create((HandlePipeAsync)))
			.WithStandardErrorPipe(PipeTarget.ToStringBuilder(sbErr));


		async Task HandlePipeAsync(Stream arg1, CancellationToken token)
		{
			var uni = await UniImage.TryCreateAsync(arg1, ct: token);

			if (uni != null) {
				rg.Add(uni);
			}

			token.ThrowIfCancellationRequested();
		}

		var cr = await cmd.ExecuteAsync(ct);

		var s2 = sbErr.ToString().Split(Environment.NewLine);

		if (!cr.IsSuccess) {
			return null;
		}

		return rg.ToArray();
	}

}