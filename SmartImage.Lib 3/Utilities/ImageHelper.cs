using System.Collections.Concurrent;
using System.Diagnostics;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Flurl.Http;
using Novus.FileTypes;

namespace SmartImage.Lib.Utilities;

internal static class ImageHelper
{
    public static async Task<UniSource[]> ScanAsync(Url u, CancellationToken ct = default)
    {
        Stream stream;

        try
        {
            stream = await u.AllowAnyHttpStatus().WithAutoRedirect(true).OnError(f =>
            {
                return;
            }).GetStreamAsync(ct);
        }
        catch (Exception e)
        {
            Debug.WriteLine($"{e.Message}");
            return Array.Empty<UniSource>();
        }

        var ul = new ConcurrentBag<UniSource>();

        var us = await UniSource.TryGetAsync(stream, whitelist: FileType.Image);

        if (us != null)
        {
            ul.Add(us);
            goto ret;
        }

        var p = new HtmlParser();
        var dd = await p.ParseDocumentAsync(stream, ct);

        var a = dd.QueryAllDistinctAttribute("a", "href");
        var b = dd.QueryAllDistinctAttribute("img", "src");

        var c = a.Union(b).Where(SearchQuery.IsValidSourceType);

        await Parallel.ForEachAsync(c, ct, async (s, token) =>
        {
            var ux = await UniSource.TryGetAsync(s, whitelist: FileType.Image);

            if (ux != null)
            {
                ul.Add(ux);

            }
            else
            {

            }

            return;
        });

        dd.Dispose();
    ret:
        return ul.ToArray();

    }

}