// Read S SmartImage.Lib BaseImageHost.cs
// 2023-07-08 @ 8:13 PM

using System.Collections.Concurrent;
using System.Diagnostics;
using AngleSharp.Html.Parser;
using Flurl.Http;
using Kantan.Net.Utilities;
using Novus.FileTypes;
using Novus.FileTypes.Uni;
using Novus.OS;
using Novus.Utilities;
using SmartImage.Lib.Model;

namespace SmartImage.Lib.Utilities;

public static class ImageScanner
{

	/*public static readonly BaseImageHost[] All =
		ReflectionHelper.CreateAllInAssembly<BaseImageHost>(InheritanceProperties.Subclass).ToArray();*/

	public static async Task<UniImage[]> ScanAsync(Url u, IImageFilter filter = null,
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
					      // f.ExceptionHandled = true;
					      return;
				      }).GetAsync(cancellationToken: ct);

		}
		catch (Exception e) {
			Debug.WriteLine($"{e.Message}");
			return [];
		}

		var ul = new ConcurrentBag<UniImage>();
		stream = await res.GetStreamAsync();
		var uf = await UniImage.TryCreateAsync(stream, t: ct);

		if (uf != null) {
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
			var ux = await UniImage.TryCreateAsync(s, t: token);

			if (ux != null) {
				/*if (!FileType.Image.Contains(ux.FileType)) {
					ux?.Dispose();
					return;
				}*/
				// Debug.WriteLine($"Found {ux.Value} for {u}", nameof(ScanAsync));
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

	public static async Task<UniImage[]> RunGalleryAsync(Url cri, CancellationToken ct = default)
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