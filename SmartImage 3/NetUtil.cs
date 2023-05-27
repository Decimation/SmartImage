using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Flurl.Http;
using Novus.FileTypes;
using SmartImage.Lib;

namespace SmartImage;

internal static class NetUtil
{
	public static async Task<UniSource[]> ScanAsync(Url u, CancellationToken ct = default)
	{
		Stream stream;

		try {
			stream = await u.AllowAnyHttpStatus().WithAutoRedirect(true).OnError(f =>
			{
				return;
			}).GetStreamAsync(ct);
		}
		catch (Exception e) {
			return Array.Empty<UniSource>();
		}

		var ul = new ConcurrentBag<UniSource>();

		var us = await UniSource.TryGetAsync(stream, whitelist: FileType.Image);

		if (us != null) {
			ul.Add(us);
			goto ret;
		}

		var p  = new HtmlParser();
		var dd = await p.ParseDocumentAsync(stream, ct);

		var a = dd.QuerySelectorAll("a")
			.Distinct()
			.Select(e => e.GetAttribute("href"))
			.Distinct();
		// .Where(e=> SearchQuery.IsValidSourceType(e));

		var b = dd.QuerySelectorAll("img")
			.Distinct()
			.Select(e => e.GetAttribute("src"))
			.Distinct();
		var c = a.Union(b);

		await Parallel.ForEachAsync(c, ct, async (s, token) =>
		{
			var ux = await UniSource.TryGetAsync(s, whitelist: FileType.Image);

			if (ux != null) {
				ul.Add(ux);

			}

			return;
		});

		dd.Dispose();
		ret:
		return ul.ToArray();
	}
}