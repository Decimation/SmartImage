using System.Collections.Concurrent;
using System.Diagnostics;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Flurl.Http;
using Novus.FileTypes;

namespace SmartImage.Lib.Utilities;

public static class ImageHelper
{
	public static async Task<UniSource[]> ScanAsync(Url u, CancellationToken ct = default)
	{
		Stream stream;

		try {
			stream = await u.AllowAnyHttpStatus().WithCookies(out var cj).WithAutoRedirect(true).OnError(f =>
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

		var us = await UniSource.TryGetAsync(stream, whitelist: FileType.Image);

		if (us != null) {
			ul.Add(us);
			goto ret;
		}

		// var p  = new HtmlParser();
		// var dd = await p.ParseDocumentAsync(stream, ct);

		var config  = Configuration.Default.WithDefaultLoader().WithCookies().WithJs().WithMetaRefresh();
		var context = BrowsingContext.New(config);
		var dd      = await context.OpenAsync(u, ct);

		var a = dd.QueryAllDistinctAttribute("a", "href");
		var b = dd.QueryAllDistinctAttribute("img", "src");

		var c = a.Union(b).Where(SearchQuery.IsValidSourceType).Distinct();

		await Parallel.ForEachAsync(c, ct, async (s, token) =>
		{
			var ux = await UniSource.TryGetAsync(s, whitelist: FileType.Image);

			if (ux != null) {
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