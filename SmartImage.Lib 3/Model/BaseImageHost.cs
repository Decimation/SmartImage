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
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Model;

public interface IImageFilter
{

	public string[] Blacklist { get; }

	public bool Refine(string b);

	public bool Predicate(UniSource us);

}

public class GenericImageFilter : IImageFilter
{

	public string[] Blacklist
		=>
		[
			"thumbnail", "avatar", "error", "logo"
		];

	public bool Predicate(UniSource us)
	{
		try {
			if (us.Stream.Length <= 25_000 || !FileType.Image.Contains(us.FileType)) {
				return false;
			}

			return true;
		}
		catch (Exception e) {
			Debug.WriteLine($"{e.Message}", nameof(Predicate));
			return true;
		}
	}

	public bool Refine(string b)
	{
		if (!Url.IsValid(b)) {
			return false;
		}

		var u  = Url.Parse(b);
		var ps = u.PathSegments;

		if (ps.Any()) {
			return !Blacklist.Any(i => ps.Any(p => p.Contains(i, StringComparison.InvariantCultureIgnoreCase)));
		}

		return true;
	}

}

public static class BaseImageHost
{

	/*public static readonly BaseImageHost[] All =
		ReflectionHelper.CreateAllInAssembly<BaseImageHost>(InheritanceProperties.Subclass).ToArray();*/

	public static async Task<UniSource[]> ScanAsync(Url u, IImageFilter filter = null,
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

		var ul = new ConcurrentBag<UniSource>();
		stream = await res.GetStreamAsync();
		var uf = await UniSource.TryGetAsync(stream, resolver: IFileTypeResolver.Default, ct: ct);

		if (uf != null) {
			/*if (!FileType.Image.Contains(uf.FileType)) {
				uf?.Dispose();
				goto ret;
			}*/
			if (FileType.Image.Contains(uf.FileType)) {
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
			var ux = await UniSource.TryGetAsync(s, ct: token);

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

	public static async Task<UniSource[]> RunGalleryAsync(Url cri, CancellationToken ct = default)
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
		var rg = new ConcurrentBag<UniSource>();

		await Parallel.ForEachAsync(s2, ct, async (s1, token) =>
		{
			var uni = await UniSource.TryGetAsync(s1, ct: token);

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