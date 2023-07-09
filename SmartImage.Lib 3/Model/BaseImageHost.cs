// Read S SmartImage.Lib BaseImageHost.cs
// 2023-07-08 @ 8:13 PM

using System.Collections.Concurrent;
using System.Diagnostics;
using AngleSharp;
using Flurl.Http;
using Novus.FileTypes;
using Novus.Utilities;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Model;

public class GenericImageHost : BaseImageHost
{
	public override Url Host => default;

	public override IEnumerable<Url> Refine(IEnumerable<Url> b)
	{
		return b.Where(x => x.PathSegments.Contains("thumbnail"));
	}
}

public abstract class BaseImageHost
{
	public abstract Url Host { get; }

	public abstract IEnumerable<Url> Refine(IEnumerable<Url> b);

	public static readonly BaseImageHost[] All =
		ReflectionHelper.CreateAllInAssembly<BaseImageHost>(TypeProperties.Subclass).ToArray();

	public static async Task<UniSource[]> ScanAsync(Url u, CancellationToken ct = default)
	{
		Stream stream;

		try {
			stream = await u.AllowAnyHttpStatus()
				         .WithCookies(out var cj)
				         .WithAutoRedirect(true)
				         .OnError(f =>
				         {
					         // f.ExceptionHandled = true;
					         return;
				         }).GetStreamAsync(ct);

		}
		catch (Exception e) {
			Debug.WriteLine($"{e.Message}");
			return Array.Empty<UniSource>();
		}

		var ul = new ConcurrentBag<UniSource>();

		var us = await UniSource.TryGetAsync(stream, whitelist: FileType.Image, ct: ct);

		if (us != null) {
			ul.Add(us);
			goto ret;
		}

		// var p  = new HtmlParser();
		// var dd = await p.ParseDocumentAsync(stream, ct);

		var config  = Configuration.Default.WithDefaultLoader().WithCookies().WithMetaRefresh();
		var context = BrowsingContext.New(config);
		var dd      = await context.OpenAsync(u, ct);

		var a = dd.QueryAllAttribute("a", "href");
		var b = dd.QueryAllAttribute("img", "src");

		var c = a.Union(b).Where(Url.IsValid).Distinct();

		var po = new ParallelOptions()
		{
			MaxDegreeOfParallelism = -1,
			CancellationToken      = ct,
		};

		await Parallel.ForEachAsync(c, po, async (s, token) =>
		{
			var ux = await UniSource.TryGetAsync(s, whitelist: FileType.Image, ct: token);

			if (ux != null) {
				Debug.WriteLine($"Found ${ux.Value} for {u}", nameof(ScanAsync));
				ul.Add(ux);

			}
			else { }

			return;
		});

		context.Dispose();
		dd.Dispose();
		ret:
		return ul.ToArray();

	}
}

public class DanbooruImageHost : GenericImageHost
{
	public override Url Host => "danbooru.donmai.us";

	public override IEnumerable<Url> Refine(IEnumerable<Url> b)
	{
		return base.Refine(b);
	}
}