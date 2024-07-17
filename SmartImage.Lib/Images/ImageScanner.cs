// Read S SmartImage.Lib BaseImageHost.cs
// 2023-07-08 @ 8:13 PM

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;
using Flurl.Http;
using Kantan.Net.Utilities;
using Novus.FileTypes;
using Novus.FileTypes.Uni;
using Novus.OS;
using Novus.Utilities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Images;

public static class ImageScanner
{

	/*public static readonly BaseImageHost[] All =
		ReflectionHelper.CreateAllInAssembly<BaseImageHost>(InheritanceProperties.Subclass).ToArray();*/

	/// <summary>
	/// Scans for images within the webpage located at <paramref name="u"/>; if <paramref name="u"/> itself
	/// points to binary image data, it is returned.
	/// </summary>
	public static async Task<UniImage[]> ScanImagesAsync(Url u, IImageFilter filter = null,
	                                                     CancellationToken ct = default)
	{
		IFlurlResponse res;
		Stream         stream;

		// pred   ??= _ => true;
		filter ??= GenericImageFilter.Instance;

		try {
			res = await u.AllowAnyHttpStatus()
				      .WithCookies(out var cj)
				      .WithAutoRedirect(true)
				      .WithHeaders(new
				      {
					      User_Agent = HttpUtilities.UserAgent
				      })
				      .OnError(f =>
				      {
					      f.ExceptionHandled = true;
					      return;
				      }).GetAsync(cancellationToken: ct);

			if (res == null) {
				return [];
			}
		}
		catch (Exception e) {
			Debug.WriteLine($"{e.Message}");
			return [];
		}

		stream = await res.GetStreamAsync();
		var ul = new ConcurrentBag<UniImage>();
		var uf = await UniImage.TryCreateAsync(stream, t: ct);

		if (uf != UniImage.Null) {
			/*if (!FileType.Image.Contains(uf.FileType)) {
				uf?.Dispose();
				goto ret;
			}*/
			if (uf.ImageFormat.DefaultMimeType != null) {
				ul.Add(uf);
				goto ret;

			}
		}
		else if (stream.CanSeek) {
			stream.Position = 0;

		}

		// var p  = new HtmlParser();
		// var dd = await p.ParseDocumentAsync(stream, ct);

		var parser = new HtmlParser();
		var doc    = await parser.ParseDocumentAsync(stream);

		// IDocument dd = await GetDocument2(u, ct);

		var c = GetImageUrls(doc, filter);

		var po = new ParallelOptions()
		{
			MaxDegreeOfParallelism = -1,
			CancellationToken      = ct,
		};

		await Parallel.ForEachAsync(c, po, async (s, token) =>
		{
			var ux = await UniImage.TryCreateAsync(s, t: token);

			if (ux != UniImage.Null) {
				/*if (!FileType.Image.Contains(ux.FileType)) {
					ux?.Dispose();
					return;
				}*/
				// Debug.WriteLine($"Found {ux.Value} for {u}", nameof(ScanForEmbeddedImagesAsync));
				if (filter.Predicate(ux)) {
					ul.Add(ux);

				}
				else {
					ux.Dispose();
				}
			}
			else { }

			return;
		});

		// context.Dispose();
		doc.Dispose();
	ret:
		return ul.ToArray();

	}

	public static async Task<IEnumerable<string>> GetImageUrls(Url u, CancellationToken token = default)
	{
		using var res = await u.AllowAnyHttpStatus()
			                .WithCookies(out var cj)
			                .WithAutoRedirect(true)
			                .WithHeaders(new
			                {
				                User_Agent = HttpUtilities.UserAgent
			                })
			                .OnError(f =>
			                {
				                f.ExceptionHandled = true;
				                return;
			                }).GetAsync(cancellationToken: token);

		var       parser = new HtmlParser();
		var stream = await res.GetStreamAsync();
		using var doc    = await parser.ParseDocumentAsync(stream);
		var       links  = GetImageUrls(doc, GenericImageFilter.Instance);
		return links;

		// await cw.WriteAsync(new SearchResultPartial(item, links), token).ConfigureAwait(false);
	}

	public static IEnumerable<string> GetImageUrls(IHtmlDocument doc, IImageFilter filter)
	{
		// var a = doc.QueryAllAttribute("a", "href");
		// var b = doc.QueryAllAttribute("img", "src");

		var a = doc.Links.Select(x => x.GetAttribute("href"));
		var b = doc.Images.Select(x => x.Source);

		var c = a.Union(b).Where(filter.Refine).Distinct();

		return c;
	}

	/*public static bool UniImagePredicate(UniImage us)
	{
		try {
			if (us.Stream.Length <= 25_000 || us.ImageFormat?.DefaultMimeType == null) {
				return false;
			}

			return true;
		}
		catch (Exception e) {
			Debug.WriteLine($"{e.Message}", nameof(UniImagePredicate));
			return true;
		}
	}*/

	// todo
	public static async Task<UniImage[]> RunGalleryDLAsync(Url cri, CancellationToken ct = default)
	{
		using var p = Process.Start(new ProcessStartInfo("gallery-dl", $"-G {cri}")
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
			var uni = await UniImage.TryCreateAsync(s1, t: token);

			if (uni != null) {
				rg.Add(uni);
			}

			token.ThrowIfCancellationRequested();
		});

		// p.Dispose();

		return rg.ToArray();
	}

	internal const string GALLERY_DL_EXE = "gallery-dl.exe";

	internal static readonly string GalleryDLPath = FileSystem.FindInPath(GALLERY_DL_EXE);

}