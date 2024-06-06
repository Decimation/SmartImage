// Read S SmartImage.Lib BaseImageHost.cs
// 2023-07-08 @ 8:13 PM

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
	public static async Task<BinaryImageFile[]> ScanImagesAsync(Url u, IImageFilter filter = null,
	                                                            CancellationToken ct = default)
	{
		IFlurlResponse res;
		Stream         stream;

		// pred   ??= _ => true;
		filter ??= new GenericImageFilter();

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

		var ul = new ConcurrentBag<BinaryImageFile>();
		stream = await res.GetStreamAsync();
		var uf = await BinaryImageFile.TryCreateAsync(stream, t: ct);

		if (uf != BinaryImageFile.Null) {
			/*if (!FileType.Image.Contains(uf.FileType)) {
				uf?.Dispose();
				goto ret;
			}*/
			if (uf.Info.DefaultMimeType != null) {
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
		var dd     = await parser.ParseDocumentAsync(stream);

		// IDocument dd = await GetDocument2(u, ct);

		var a = dd.QueryAllAttribute("a", "href");
		var b = dd.QueryAllAttribute("img", "src");

		var c = a.Union(b).Where(filter.Refine).Distinct();

		var po = new ParallelOptions()
		{
			MaxDegreeOfParallelism = -1,
			CancellationToken      = ct,
		};

		await Parallel.ForEachAsync(c, po, async (s, token) =>
		{
			var ux = await BinaryImageFile.TryCreateAsync(s, t: token);

			if (ux != BinaryImageFile.Null) {
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
		dd.Dispose();
	ret:
		return ul.ToArray();

	}

	public static bool UniSourcePredicate(UniSource us)
	{
		try {
			if (us.Stream.Length <= 25_000 || !FileType.Image.Contains(us.FileType)) {
				return false;
			}

			return true;
		}
		catch (Exception e) {
			Debug.WriteLine($"{e.Message}", nameof(UniSourcePredicate));
			return true;
		}
	}

	// todo
	public static async Task<BinaryImageFile[]> RunGalleryAsync(Url cri, CancellationToken ct = default)
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
		var rg = new ConcurrentBag<BinaryImageFile>();

		await Parallel.ForEachAsync(s2, ct, async (s1, token) =>
		{
			var uni = await BinaryImageFile.TryCreateAsync(s1, t: token);

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