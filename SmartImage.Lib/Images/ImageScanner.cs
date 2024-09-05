// Read S SmartImage.Lib BaseImageHost.cs
// 2023-07-08 @ 8:13 PM

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Web;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;
using Flurl.Http;
using Jint.Parser;
using Kantan.Net.Utilities;
using Novus.FileTypes;
using Novus.FileTypes.Uni;
using Novus.OS;
using Novus.Streams;
using Novus.Utilities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;

// ReSharper disable InconsistentNaming

namespace SmartImage.Lib.Images;

public static class ImageScanner
{

	public static FlurlClient Client { get; }

	static ImageScanner()
	{
		Client = (FlurlClient) FlurlHttp.Clients.GetOrAdd(nameof(ImageScanner), null, builder =>
		{
			builder.Settings.AllowedHttpStatusRange = "*";
			builder.Headers.AddOrReplace("User-Agent", HttpUtilities.UserAgent);
			builder.AllowAnyHttpStatus();

			builder.OnError(f =>
			{
				f.ExceptionHandled = true;
				return;
			});

		});

	}

	/*public static readonly BaseImageHost[] All =
		ReflectionHelper.CreateAllInAssembly<BaseImageHost>(InheritanceProperties.Subclass).ToArray();*/

	/// <summary>
	/// Scans for images within the webpage located at <paramref name="u"/>; if <paramref name="u"/> itself
	/// points to binary image data, it is returned.
	/// </summary>
	public static async Task<UniImage[]> ScanImagesAsync(Url u, IImageFilter filter = null,
	                                                         CancellationToken ct = default)
	{

		IFlurlResponse          res;
		Stream                  stream;
		ConcurrentBag<UniImage> rg = [];
		
		// pred   ??= _ => true;
		filter ??= GenericImageFilter.Instance;

		try {
			res = await Client.Request(u)
				      .WithCookies(out var cj)
				      .GetAsync(cancellationToken: ct);

			if (res == null) {
				// return [];
				goto ret;
			}
		}
		catch (Exception e) {
			Debug.WriteLine($"{e.Message}");

			// return [];
			goto ret;
		}

		stream = await res.GetStreamAsync();
		var uf = await UniImage.TryCreateAsync(stream, ct: ct);

		if (uf != UniImage.Null) {
			/*if (!FileType.Image.Contains(uf.FileType)) {
				uf?.Dispose();
				goto ret;
			}*/
			if (uf.HasImageFormat) {
				// ul.Add(uf);
				// rg = [uf];
				rg.Add(uf);
				goto ret;

			}
		}
		else if (stream.CanSeek) {
			stream.Position = 0;

		}

		// var p  = new HtmlParser();
		// var dd = await p.ParseDocumentAsync(stream, ct);

		// var parser = new HtmlParser();
		// var doc    = await parser.ParseDocumentAsync(stream);

		// IDocument dd = await GetDocument2(u, ct);

		// var c = GetImageUrls(doc, filter);
		// string html = await res.GetStringAsync();
		if (!stream.CanRead) {
			stream?.Dispose();
			goto ret;
		}
		
		var    sr   = new StreamReader(stream, leaveOpen: true);
		string html = await sr.ReadToEndAsync(ct).ConfigureAwait(false);
		
		// var html = doc.ToString();
		var urls = GetImageUrls(html, u);

		var po = new ParallelOptions()
		{
			MaxDegreeOfParallelism = -1,
			CancellationToken      = ct,
		};

		foreach (string s in urls) {
			var ux = await UniImage.TryCreateAsync(s, ct: ct);

			if (ux != UniImage.Null) {
				/*if (!FileType.Image.Contains(ux.FileType)) {
					ux?.Dispose();
					return;
				}*/
				// Debug.WriteLine($"Found {ux.Value} for {u}", nameof(ScanForEmbeddedImagesAsync));

				if ((filter != null && filter.Predicate(ux)) || filter == null) {
					// ul.Add(ux);
					rg.Add(ux);
				}
				else {
					ux.Dispose();
					ux = null;
				}
			}
			else { }

		}

	ret1:

		// context.Dispose();
		// doc.Dispose();
		sr.Dispose();
		res.Dispose();

	ret:
		return rg.ToArray();

	}


	/*
	public static async Task<IEnumerable<string>> GetImageUrlsAsync(Url u, IImageFilter filter = null,
	                                                                CancellationToken token = default)
	{
		using var res = await Client.Request(u)
			                .WithCookies(out var cj)
			                .GetAsync(cancellationToken: token);


		// filter ??= GenericImageFilter.Instance;

		var       parser = new HtmlParser();
		var       stream = await res.GetStreamAsync();
		using var doc    = await parser.ParseDocumentAsync(stream);
		var       links  = GetImageUrls(doc, filter);
		return links;

		// await cw.WriteAsync(new SearchResultPartial(item, links), token).ConfigureAwait(false);
	}
	*/


	public static async Task<UniImage[]> RunGalleryDLAsync(Url cri, CancellationToken ct = default)
	{
		using var p = Process.Start(new ProcessStartInfo(GALLERY_DL, $"-G {cri}")
		{
			CreateNoWindow         = true,
			RedirectStandardOutput = true,
			RedirectStandardError  = true,
		});
		await p.WaitForExitAsync(ct);
		var s  = await p.StandardOutput.ReadToEndAsync(ct);
		var s2 = s.Split(Environment.NewLine);
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

	internal const string GALLERY_DL     = "gallery-dl";
	internal const string GALLERY_DL_EXE = $"{GALLERY_DL}.exe";

	private const char URL_DELIM = '/';

	internal static readonly string GalleryDLPath = FileSystem.FindInPath(GALLERY_DL_EXE);

	/*
	 * TODO:
	 *
	 * Aggregate
	 * Highest
	 *
	 * Gallery-DL
	 */

	private static readonly Regex r_imgSource = new(
		"""(?i)<(?:img|video|source)\s[^>]*src(?:set)?=[\"]?(?<URL>[^\"\s>]+)""",
		RegexOptions.Compiled
	);

	private static readonly Regex r_imgExt = new(
		@"(?i)(?:[^?&#""'>\s]+)\.(?:jpe?g|jpe|png|gif|web[mp]|mp4|mkv|og[gmv]|opus)(?:[^""'<>\s]*)?",
		RegexOptions.Compiled
	);

	public static IEnumerable<string> GetImageUrls(string html, Url url)
	{
		var imgUrlsSrc = r_imgSource.Matches(html).Select(m => m.Groups["URL"].Value);
		var imgUrlsExt = r_imgExt.Matches(html).Select(m => m.Value);
		var imgUrls    = imgUrlsSrc.Concat(imgUrlsExt);

		var    baseMatch = Regex.Match(html, """(?i)(?:<base\s.*?href=[\"]?)(?<url>[^\"' >]+)""");
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
				return url.Scheme + u.TrimStart(URL_DELIM);

			if (u.StartsWith(URL_DELIM))
				return url.Root + u;

			return baseUrl + URL_DELIM + u;
		}).Where(Url.IsValid).Distinct();

		return abs;
	}

	public static IEnumerable<string> GetImageUrls(IHtmlDocument doc, IImageFilter filter = null)
	{
		// var a = doc.QueryAllAttribute("a", "href");
		// var b = doc.QueryAllAttribute("img", "src");

		var a = doc.Links.Select(x => x.GetAttribute("href"));
		var b = doc.Images.Select(x => x.Source);

		var c = a.Union(b);

		if (filter != null) {
			c = c.Where(filter.Refine);
		}

		c = c.Distinct();

		return c;
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

}