// Read S SmartImage.Lib BaseImageHost.cs
// 2023-07-08 @ 8:13 PM

using System.Collections.Concurrent;
using System.Diagnostics;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Flurl.Http;
using Kantan.Net.Utilities;
using Novus.FileTypes;
using Novus.Utilities;
using SmartImage.Lib.Engines;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Model;

public class GenericImageHost : BaseImageHost
{
	public override        Url           Host => default;
	public static readonly BaseImageHost Instance = new GenericImageHost();

	public override string[] Illegal
		=> new[]
		{
			"thumbnail", "avatar", "error", "logo"
		};

	public override bool Refine(string b)
	{
		if (!Url.IsValid(b)) {
			return false;
		}

		var u  = Url.Parse(b);
		var ps = u.PathSegments;

		if (ps.Any()) {
			return !Illegal.Any(i => ps.Any(p => p.Contains(i, StringComparison.InvariantCultureIgnoreCase)));
		}

		return true;
		;
	}
}

public abstract class BaseImageHost
{
	public abstract Url Host { get; }

	public abstract string[] Illegal { get; }

	public abstract bool Refine(string b);

	public static readonly BaseImageHost[] All =
		ReflectionHelper.CreateAllInAssembly<BaseImageHost>(TypeProperties.Subclass).ToArray();

	public static async Task<UniSource[]> ScanAsync(Url u, Predicate<UniSource> pred = null,
	                                                CancellationToken ct = default)
	{
		Stream stream;
		pred ??= _ => true;

		try {
			stream = await u.AllowAnyHttpStatus()
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
		else if (stream.CanSeek) {
			stream.Position = 0;

		}

		// var p  = new HtmlParser();
		// var dd = await p.ParseDocumentAsync(stream, ct);
	
		var parser = new HtmlParser();
		var dd     = await parser.ParseDocumentAsync(stream);

		// IDocument dd = await GetDocument2(u, ct);

		//find all attribute in any element...
		//where the value ends with one of the listed file extension
		var result = from element in dd.All
		             from attribute in element.Attributes
		             where ImageHelper.Ext.Any(e => attribute.Value.EndsWith(e))
		             select attribute;

		var a = dd.QueryAllAttribute("a", "href");
		var b = dd.QueryAllAttribute("img", "src");

		var c = a.Union(b).Where(GenericImageHost.Instance.Refine).Distinct();

		var po = new ParallelOptions()
		{
			MaxDegreeOfParallelism = -1,
			CancellationToken      = ct,
		};

		await Parallel.ForEachAsync(c, po, async (s, token) =>
		{
			var ux = await UniSource.TryGetAsync(s, whitelist: FileType.Image, ct: token);

			if (ux != null) {
				// Debug.WriteLine($"Found {ux.Value} for {u}", nameof(ScanAsync));
				if (pred(ux)) {
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

	private static async Task<IDocument> GetDocument2(Url u, CancellationToken ct)
	{
		var dd = await u.AllowAnyHttpStatus()
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
			         }).GetStreamAsync(ct);
		var parser = new HtmlParser();

		var doc = await parser.ParseDocumentAsync(dd);

		return doc;
	}

	private static async Task<IDocument> GetDocument(Url u, CancellationToken ct)
	{
		var config = Configuration.Default.WithDefaultLoader().WithCookies().WithRequesters().WithJs()
			.WithMetaRefresh();
		var context = BrowsingContext.New(config);
		var dd      = await context.OpenAsync(u, ct);
		context.Dispose();
		return dd;
	}

	public static bool UniSourcePredicate(UniSource us)
	{
		try {
			if (us.Stream.Length <= 25_000) {
				return false;
			}

			return true;
		}
		catch (Exception e) {
			return true;
		}
	}
}

public class DanbooruImageHost : GenericImageHost
{
	public override Url Host => "danbooru.donmai.us";

	public override bool Refine(string b)
	{
		return base.Refine(b);
	}
}