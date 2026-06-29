// Read S SmartImage.Lib BaseImageHost.cs
// 2023-07-08 @ 8:13 PM

using AngleSharp.Html.Dom;
using CliWrap;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Utilities;
using SmartImage.Lib.Utilities.Integration;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using AngleSharp.Io;
using SmartImage.Lib.Engines.Results;

// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

// ReSharper disable UnusedMember.Global
// ReSharper disable SuggestVarOrType_Elsewhere
// ReSharper disable InconsistentNaming

#pragma warning disable CA1041

namespace SmartImage.Lib.Images;

public static partial class ImageScanner
{

	public static FlurlClient Client { get; }

	private static readonly ILogger s_logger;

	static ImageScanner()
	{
		s_logger = AppSupport.Factory.CreateLogger(nameof(ImageScanner));

		Client = (FlurlClient) FlurlHttp.Clients.GetOrAdd(nameof(ImageScanner), null, static builder =>
		{
			// builder.Settings.Redirects.ForwardAuthorizationHeader = true;
			// builder.Settings.Redirects.AllowSecureToInsecure      = true;

			builder.Settings.AllowedHttpStatusRange = "*";
			builder.Settings.HttpVersion            = "2.0";

			builder.Headers.AddOrReplace(HeaderNames.UserAgent, R1.UserAgent1);

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

	public static readonly string[] FormatExtensions = [.. ImageFormats.SelectMany(static fmt => fmt.FileExtensions)];

#endregion

#region Scanning/parsing

	public static readonly string[] UrlSegmentBlacklist = ["thumbs", ".svg", ".ico", "twitter.svg", "pinterest.svg", "favicon"];

	public static readonly string[] LegalSchemeWhitelist = ["http", "https"];

	internal const char URL_DELIM = '/';


	/// <summary>
	/// Parses image URLs using adapted <em><c>gallery-dl</c></em> regex 
	/// </summary>
	public static IEnumerable<string> ParseImageUrls(string html, Url url, bool heuristicFilter = true)
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

		imgUrls = imgUrls.Distinct().Where(static e =>
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
			if (u.StartsWith(LegalSchemeWhitelist[0]))
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

	/// <summary>
	/// Parses image URLS using <see cref="IHtmlDocument.Links"/> and <see cref="IHtmlDocument.Images"/>
	/// </summary>
	public static IEnumerable<string> ParseImageUrls(IHtmlDocument doc)
	{
		var links  = doc.Links.Select(static x => x.GetAttribute(Serialization.Atr_href));
		var images = doc.Images.Select(static x => x.Source);
		var union  = links.Union(images).Distinct();

		return union;
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

	public static async Task RunGalleryDLAsync(Url cri, ChannelWriter<Url> cw, CancellationToken ct = default)
	{
		// TODO: TEST

		if (!BaseOSIntegration.Integration.IsGalleryDLInstalled) {
			goto ret;
		}

		var sbErr = new StringBuilder();
		var sbOut = new StringBuilder();

		// ReSharper disable once AssignNullToNotNullAttribute
		var cmd = Cli.Wrap(BaseOSIntegration.Integration.GalleryDLPath)
		             .WithArguments([$"-G", cri])
		             .WithValidation(CommandResultValidation.None)
		             .WithStandardOutputPipe(PipeTarget.ToDelegate(HandleLineAsync))
		             .WithStandardErrorPipe(PipeTarget.ToStringBuilder(sbErr));


		var cr = await cmd.ExecuteAsync(ct);
		var s2 = sbErr.ToString().Split(Environment.NewLine);

		if (!cr.IsSuccess) {
			Debugger.Break();
			goto ret;
		}

	ret:
		cw.TryComplete();

		return;

		Task HandleLineAsync(string s)
		{
			return cw.WriteAsync(s).AsTask();
		}

		/*async Task HandleLineAsync(string s, CancellationToken token)
		{
			var uni = await UniImage.FromSourceAsync(s, ct: token);

			// var uni = await ScannedResultItem.FromResult(s, new ScannedResultItem(cri, null), token);
			var b = cw.TryWrite(uni);

			if (uni != null) {
			}

			token.ThrowIfCancellationRequested();
		}*/
	}

}