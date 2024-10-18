// Read S SmartImage.Lib BaseImageHost.cs
// 2023-07-08 @ 8:13 PM

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Io;
using CliWrap;
using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;
using Flurl.Http;
using Jint.Parser;
using Kantan.Net.Utilities;
using Microsoft.Win32;
using Novus.FileTypes;
using Novus.FileTypes.Uni;
using Novus.OS;
using Novus.Streams;
using Novus.Utilities;
using Novus.Win32.Structures.Other;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;
using VerifyTests.Http;

// ReSharper disable InconsistentNaming

namespace SmartImage.Lib.Images;

public static class ImageScanner
{

	static ImageScanner()
	{
		Client = (FlurlClient) FlurlHttp.Clients.GetOrAdd(nameof(ImageScanner), null, builder =>
		{
			// builder.Settings.Redirects.ForwardAuthorizationHeader = true;
			// builder.Settings.Redirects.AllowSecureToInsecure      = true;

			builder.Settings.AllowedHttpStatusRange = "*";
			builder.Headers.AddOrReplace("User-Agent", HttpUtilities.UserAgent);
			builder.AllowAnyHttpStatus();
			builder.WithAutoRedirect(true);

			builder.OnError(f =>
			{
				f.ExceptionHandled = true;
				return;
			});

		});
		Cookies = new CookieJar();
	}

	public static FlurlClient Client { get; }

	public static CookieJar Cookies { get; }

	/*public static IFlurlRequest BuildRequest(params object[] urlSeg)
	{
		var request = Client.Request(urlSeg);

		if (r_donmai.IsMatch(request.Url.Host)) {
			request.Headers.AddOrReplace("User-Agent", R1.Name);
		}

		return request
			.WithCookies(Cookies);
	}*/

	private static bool r_donmaiInit;

	public static async ValueTask<IFlurlRequest> BuildRequest(Url u, CancellationToken ct = default)
	{
		var req = Client.Request(u);

		if (r_donmai.IsMatch(req.Url.Host)) {
			if (!r_donmaiInit) {
				var req2 = Client.Request(req.Url);

				req2.Headers.AddOrReplace("User-Agent", R1.Name);

				using (var res2 = await req2.WithCookies(Cookies).GetAsync(cancellationToken: ct)) {
					Debugger.Break();
				}

				r_donmaiInit = true;

			}
			else { }
		}

		return req
			.WithCookies(Cookies);
	}

	public static async Task<IReadOnlyList<FlurlCookie>> GetCookies(IFlurlRequest req, CancellationToken ct = default)
	{
		IReadOnlyList<FlurlCookie> ret = [];

		if (r_donmai.IsMatch(req.Url.Host)) {

			req.Headers.AddOrReplace("User-Agent", R1.Name);

			using (var res2 = await req.GetAsync(cancellationToken: ct)) {
				ret = res2.Cookies;
			}
		}

		return ret;
	}

	/*public static readonly BaseImageHost[] All =
		ReflectionHelper.CreateAllInAssembly<BaseImageHost>(InheritanceProperties.Subclass).ToArray();*/


	/// <summary>
	/// Scans for images within the webpage located at <paramref name="u"/>; if <paramref name="u"/> itself
	/// points to binary image data, it is returned.
	/// </summary>
	public static async Task<List<Task<UniImage>>> ScanImagesAsync(Url u, CancellationToken ct = default)
	{

		List<Task<UniImage>> tasks = null;
		IFlurlRequest        req;
		IFlurlResponse       res;
		Stream               stream = Stream.Null;

		// req = BuildRequest(u);

		// var ck = await GetCookies(req, ct);

		/*foreach (var c in ck) {
			Cookies.AddOrReplace(c);
		}*/
		/*req=req.OnRedirect(r=>
		{
			Debug.WriteLine($"{r.Redirect} {r.Response}");
		});*/
		// res = await req.GetAsync(cancellationToken: ct);


		// stream = await res.GetStreamAsync();
		/*var isUri = UniImageUri.IsUriType(u, out u);

		if (isUri) {
			var uf      = new UniImageUri(u, u);
			var allocOk = await uf.Alloc(ct);

			if (allocOk) {

				stream = uf.Stream;

				var rsrcHdr   = await FileType.ReadResourceHeaderAsync(stream, ct);
				var rsrcHdrRg = rsrcHdr.ToArray();
				var binRsrc   = FileType.IsBinaryResource(rsrcHdrRg);

				stream.TrySeek();

				switch (binRsrc) {
					case FileType.MT_APPLICATION_OCTET_STREAM:
					{
						var dfOk = await uf.DetectFormat(ct);

						if (dfOk) {
							tasks = [Task.FromResult((UniImage) uf)];
							goto ret;
						}

						uf.Dispose();

						break;
					}

					case FileType.MT_TEXT_PLAIN:
					{
						var sr = new StreamReader(stream, leaveOpen: true /*false#1#);

						string html = await sr.ReadToEndAsync(ct);
						var    urls = GetImageUrls(html, u);

						tasks = urls.Select(s =>
						{
							var ux = UniImage.TryCreateAsync(s, ct: ct);

							return ux;

						}).ToList();

						goto ret;
					}
				}
			}
			else {
				Debugger.Break();

				// throw new Exception();
			}
		}*/

		var uf = await UniImage.TryCreateAsync(u, autoInit: true,
		                                       autoDisposeOnError: false, ct: ct);


		if (uf != UniImage.Null) {
			if (uf.HasImageFormat) {
				tasks = [Task.FromResult(uf)];

				goto ret;
			}
		}
		else {
			stream          = uf.Stream;
			stream.Position = 0;

		}

		if (!stream.CanRead) {
			stream.Dispose();
			goto ret;
		}

		var sr = new StreamReader(stream, leaveOpen: true /*false*/);

		string html = await sr.ReadToEndAsync(ct);
		var    urls = GetImageUrls(html, u);

		tasks = urls.Select(s =>
		{
			var ux = UniImage.TryCreateAsync(s, ct: ct);

			return ux;

		}).ToList();

		// var rr = await u.WithHeader("User-Agent", "SI").GetStreamAsync(HttpCompletionOption.ResponseContentRead);


		// var parser = new HtmlParser();

		// var doc    = await parser.ParseDocumentAsync(stream);

		// var html = await Client.Request(res.ResponseMessage.RequestMessage.RequestUri).GetStringAsync();


		// var html = await res.GetStringAsync();

		// var html = doc.ToString();


		/*var po = new ParallelOptions();

		await Parallel.ForEachAsync(urls, po, async (s, token) =>
		{
			var ux = await UniImage.TryCreateAsync(s, ct: token).ConfigureAwait(false);

			if (ux != UniImage.Null) {
				/*if (!FileType.Image.Contains(ux.FileType)) {
					ux?.Dispose();
					return;
				}#1#
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
		});*/


	ret1:

		// doc.Dispose();
		// sr.Dispose();

		// stream.Dispose(); // todo?
		// res.Dispose();

	ret:
		return tasks;

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
	public static HttpMessageHandler GetMostInnerHandler(this HttpMessageHandler self)
	{
		return self is DelegatingHandler handler
			       ? handler.InnerHandler.GetMostInnerHandler()
			       : self;
	}
	public static IFlurlRequest AddChromeImpersonation(this IFlurlRequest req)
	{
		return req.WithHeaders(new
		{
			sec_ch_ua = "\"Chromium\";v=\"104\", \" Not A;Brand\";v=\"99\", \"Google Chrome\";v=\"104\"",
			sec_ch_ua_mobile = "?0",
			sec_ch_ua_platform = "Windows",
			Upgrade_Insecure_Requests = "1",
			User_Agent =
				"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.0.0 Safari/537.36",
			Accept =
				"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9",
			Sec_Fetch_Site  = "none",
			Sec_Fetch_Mode  = "navigate",
			Sec_Fetch_User  = "?1",
			Sec_Fetch_Dest  = "document",
			Accept_Encoding = "gzip, deflate, br",
			Accept_Language = "en-US,en;q=0.9"
		});
	}

	public static async Task<List<UniSimilarity>> Analyze(List<Task<UniImage>> tasks, SearchQuery query,
	                                                      CancellationToken ct = default)
	{
		var ph   = new PerceptualHash();
		var orig = ph.Hash(query.Uni.Stream);
		query.Uni.Stream.TrySeek();
		var rg = new List<UniSimilarity>();

		while (tasks.Count != 0) {
			var task = await Task.WhenAny(tasks);
			tasks.Remove(task);
			var ux = await task;

			if (ux != UniImage.Null && ux.HasImageFormat) {
				var cmp = ph.Hash(ux.Stream);
				var sim = CompareHash.Similarity(orig, cmp);
				rg.Add(new UniSimilarity(ux, sim));
				ux.Stream.TrySeek();
			}
			else {
				ux.Dispose();
				ux = null;
			}
		}

		return rg;
	}
	/*
	public static async IAsyncEnumerable<UniImage> ScanImagesAsync2(Url u, IImageFilter filter = null,
	                                                                [EnumeratorCancellation]
	                                                                CancellationToken ct = default)
	{

		var tasks = await ScanImagesAsync(u, ct);

		while (tasks.Count != 0) {
			var task = await Task.WhenAny(tasks);
			tasks.Remove(task);
			var ux = await task;

			if (ux != UniImage.Null) {
				if ((filter != null && filter.Predicate(ux)) || filter == null) {

					yield return ux;
				}
				else {
					ux.Dispose();
					ux = null;
				}
			}
			else { }
		}

	}
	*/


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


	private const char URL_DELIM = '/';

	internal static readonly Regex r_donmai = new(
		"""\.donmai\.us""",
		RegexOptions.Compiled
	);

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
		"""(?i)(?:[^?&#"'>\s]+)\.(?:jpe?g|jpe|png|gif|web[mp]|mp4|mkv|og[gmv]|opus)(?:[^"'<>\s]*)?""",
		RegexOptions.Compiled
	);

	private static readonly Regex r_imgHtml = new(
		"""(?i)(?:<base\s.*?href=[\"]?)(?<url>[^\"' >]+)""",
		RegexOptions.Compiled
	);


	public static IEnumerable<string> GetImageUrls(string html, Url url)
	{
		var imgUrlsSrc = r_imgSource.Matches(html).Select(m => m.Groups["URL"].Value);
		var imgUrlsExt = r_imgExt.Matches(html).Select(m => m.Value);
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
				return url.Scheme + u.TrimStart(URL_DELIM);

			if (u.StartsWith(URL_DELIM))
				return url.Root + u;

			return baseUrl + URL_DELIM + u;
		}).Where(Url.IsValid).Distinct();

		return abs;
	}

	public static IEnumerable<string> GetImageUrls(IHtmlDocument doc)
	{
		// var a = doc.QueryAllAttribute("a", "href");
		// var b = doc.QueryAllAttribute("img", "src");

		var a = doc.Links.Select(x => x.GetAttribute("href"));
		var b = doc.Images.Select(x => x.Source);

		var c = a.Union(b);


		c = c.Distinct();

		return c;
	}

	#region

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

	internal static readonly string GalleryDLPath = FileSystem.FindInPath(GALLERY_DL_EXE);

	#endregion

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