using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Flurl.Http;
using SixLabors.ImageSharp.Formats;
using SmartImage.Lib.Images;
using SmartImage.Lib.Results;

namespace SmartImage.Lib.Utilities;

public static class SearchOperations
{

	// TODO: WIP

	public static async Task<IEnumerable<SearchResultItem>> Aggregate(IEnumerable<SearchResultItem> results)
	{
		var groups = results.GroupBy(g => new { g.Artist });

		foreach (var v in groups) {
			Console.WriteLine($"{v.Key}");
		}

		return ( []);
	}

	public class Item2
	{

		public SearchResultItem Item { get; init; }

		public IImageFormat Image { get; init; }

		public string Url { get; init; }

	}


	public static async Task<IEnumerable<Item2>> Highest(SearchQuery query,
														 IEnumerable<SearchResultItem> results,
														 CancellationToken ct = default)
	{
		var plr = new ParallelOptions()
		{
			CancellationToken      = ct,
			MaxDegreeOfParallelism = 10,
		};

		/*await Parallel.ForEachAsync(results, plr, async (item, token) =>
		{
			var r = await ImageScanner.GetImageUrlsAsync(item.Url, token: token);
			Debug.WriteLine($"{item.Url} -> {r}");

		});*/

		results = results.Where(r => !r.IsRaw && r.Url != null);

		var cb = new ConcurrentBag<Item2>();

		foreach (var result in results) {

			// IDocument dd = await GetDocument2(u, ct);


			var req = await ImageScanner.Client.Request(result.Url)
						  .GetAsync(cancellationToken: ct);
			var stream = await req.GetStreamAsync();

			var parser = new HtmlParser();
			var doc    = await parser.ParseDocumentAsync(stream);

			var urls = ImageScanner.GetImageUrls(doc);

			doc.Dispose();
			req.Dispose();

			var urls2 = urls as string[] ?? urls.ToArray();

			Debug.WriteLine($"{result.Url} -> {urls2.Length}");

			await Parallel.ForEachAsync(urls2, plr, async (s, token) =>
			{
				var resp = await ImageScanner.Client.Request(s)
							   .OnError(call =>
							   {
								   call.ExceptionHandled = true;
							   })
							   .WithHeaders(new
							   {
								   // todo
								   User_Agent = R1.UserAgent1,
							   })
							   .GetAsync(cancellationToken: ct);

				var bin = await resp.GetStreamAsync();

				try {
					var img = await ISImage.DetectFormatAsync(bin, token);
					cb.Add(new Item2() { Image = img, Item = result });

					// Debug.WriteLine($"{img}");
				}
				catch (Exception e) {
					Debug.WriteLine(e);
				}

				resp.Dispose();

			});
		}

		return cb;
	}

}

#if GE_TEST

public class GenericExtractor
{

	public string Category { get; private set; } = "generic";

	public string Url { get; private set; }

	public string Root { get; private set; }

	public string Scheme { get; private set; } = "https://";

/*
 * pattern = r"(?i)(?P<generic>g(?:eneric)?:)"
	  if config.get(("extractor", "generic"), "enabled"):
		  pattern += r"?"

	  // The generic extractor pattern should match (almost) any valid url
	  // Based on: https://tools.ietf.org/html/rfc3986#appendix-B
	  pattern += (
		  r"(?P<scheme>https?://)?"          # optional http(s) scheme
		  r"(?P<domain>[-\w\.]+)"            # required domain
		  r"(?P<path>/[^?#]*)?"              # optional path
		  r"(?:\?(?P<query>[^#]*))?"         # optional query
		  r"(?:\#(?P<fragment>.*))?"         # optional fragment
	  )
 */
	private static Regex s_pattern = new(

		// @"(?i)(?P<generic>g(?:eneric)?:)?(?P<scheme>https?://)?(?P<domain>[-\w\.]+)(?P<path>/[^?#]*)?(?:\?(?P<query>[^#]*))?(?:\#(?P<fragment>.*))?",
/*
 *pattern += (
	   r"(?P<scheme>https?://)?"          # optional http(s) scheme
	   r"(?P<domain>[-\w\.]+)"            # required domain
	   r"(?P<path>/[^?#]*)?"              # optional path
	   r"(?:\?(?P<query>[^#]*))?"         # optional query
	   r"(?:\#(?P<fragment>.*))?"         # optional fragment
   )
 */
		"(?i)(?P<generic>g(?:eneric)?:)?(?P<scheme>https?://)?(?P<domain>[-\\w\\.]+)(?P<path>/[^?#]*)?(?:\\?(?P<query>[^#]*))?(?:\\#(?P<fragment>.*))?",
		// "(?i)(?P<generic>g(?:eneric)?:)?",
		RegexOptions.Compiled
	);

	public GenericExtractor(Match match)
	{
		if (match.Groups["generic"].Success) {
			Url = match.Value.Substring(match.Value.IndexOf(":") + 1);
		}
		else {
			Console.WriteLine("Falling back on generic information extractor.");
			Url = match.Value;
		}

		if (match.Groups["scheme"].Success) {
			Scheme = match.Groups["scheme"].Value;
		}
		else {
			Url = Scheme + Url;
		}

		Root = Scheme + match.Groups["domain"].Value;

		/*
		 * pattern += (
					r"(?P<scheme>https?://)?"          # optional http(s) scheme
					r"(?P<domain>[-\w\.]+)"            # required domain
					r"(?P<path>/[^?#]*)?"              # optional path
					r"(?:\?(?P<query>[^#]*))?"         # optional query
					r"(?:\#(?P<fragment>.*))?"         # optional fragment
				)
		 */
	}

	public async IAsyncEnumerable<Message> Items()
	{
		string page = await Request(Url);
		var    data = Metadata(page);
		var    imgs = Images(page);

		try {
			data["count"] = imgs.Count().ToString();
		}
		catch {
			// Handle TypeError if needed
		}

		yield return new Message { Type = MessageType.Directory, Data = data };

		int num = 1;

		foreach (var (url, imgdata) in imgs) {
			if (imgdata != null) {
				data = data.Concat(imgdata).ToDictionary(k => k.Key, v => v.Value);

				if (!imgdata.ContainsKey("extension")) {
					NameextFromUrl(url, data);
				}
			}
			else {
				NameextFromUrl(url, data);
			}

			data["num"] = num.ToString();
			yield return new Message { Type = MessageType.Url, Url = url, Data = data };

			num++;
		}
	}

	private Dictionary<string, string> Metadata(string page)
	{
		var data = new Dictionary<string, string>
		{
			["pageurl"]        = Url,
			["title"]          = Extract(page, "<title>", "</title>"),
			["description"]    = Extract(page, "<meta name=\"description\" content=\"", "\""),
			["keywords"]       = Extract(page, "<meta name=\"keywords\" content=\"", "\""),
			["language"]       = Extract(page, "<meta name=\"language\" content=\"", "\""),
			["name"]           = Extract(page, "<meta itemprop=\"name\" content=\"", "\""),
			["copyright"]      = Extract(page, "<meta name=\"copyright\" content=\"", "\""),
			["og_site"]        = Extract(page, "<meta property=\"og:site\" content=\"", "\""),
			["og_site_name"]   = Extract(page, "<meta property=\"og:site_name\" content=\"", "\""),
			["og_title"]       = Extract(page, "<meta property=\"og:title\" content=\"", "\""),
			["og_description"] = Extract(page, "<meta property=\"og:description\" content=\"", "\"")
		};

		return data.Where(kv => !string.IsNullOrEmpty(kv.Value)).ToDictionary(kv => kv.Key, kv => kv.Value);
	}

	private IEnumerable<(string url, Dictionary<string, string> imgdata)> Images(string page)
	{
		var imageurlPatternSrc = new Regex(
			@"(?i)<(?:img|video|source)\s[^>]*src(?:set)?=[\""]?(?<URL>[^\""\s>]+)",
			RegexOptions.Compiled
		);

		var imageurlPatternExt = new Regex(
			@"(?i)(?:[^?&#""'>\s]+)\.(?:jpe?g|jpe|png|gif|web[mp]|mp4|mkv|og[gmv]|opus)(?:[^""'<>\s]*)?",
			RegexOptions.Compiled
		);

		var imageurlsSrc = imageurlPatternSrc.Matches(page).Select(m => m.Groups["URL"].Value);
		var imageurlsExt = imageurlPatternExt.Matches(page).Select(m => m.Value);
		var imageurls    = imageurlsSrc.Concat(imageurlsExt);

		var basematch = Regex.Match(page, @"(?i)(?:<base\s.*?href=[\""]?)(?<url>[^\""' >]+)");

		string baseurl = basematch.Success
							 ? basematch.Groups["url"].Value.TrimEnd('/')
							 : (Url.EndsWith("/") ? Url.TrimEnd('/') : Path.GetDirectoryName(Url));

		var absimageurls = imageurls.Select(u =>
		{
			if (u.StartsWith("http"))
				return u;

			if (u.StartsWith("//"))
				return Scheme + u.TrimStart('/');

			if (u.StartsWith("/"))
				return Root + u;

			return baseurl + "/" + u;
		}).Distinct();

		return absimageurls.Select(u => (u, new Dictionary<string, string> { ["imageurl"] = u }));
	}

	private async Task<string> Request(string url)
	{
		using (var client = new HttpClient()) {
			return await client.GetStringAsync(url);
		}
	}

	private string Extract(string page, string start, string end)
	{
		int startIndex = page.IndexOf(start);

		if (startIndex == -1)
			return string.Empty;

		startIndex += start.Length;
		int endIndex = page.IndexOf(end, startIndex);
		return endIndex == -1 ? string.Empty : page.Substring(startIndex, endIndex - startIndex);
	}

	private void NameextFromUrl(string url, Dictionary<string, string> data)
	{
		// Implement name and extension extraction from URL logic here
	}

}

public class Message
{

	public MessageType Type { get; set; }

	public string Url { get; set; }

	public Dictionary<string, string> Data { get; set; }

}

public enum MessageType
{

	Directory,
	Url

}

#endif