// ReSharper disable UnusedMember.Global

using System.Diagnostics;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Flurl.Http;
using Kantan.Net.Utilities;
using Kantan.Text;

// ReSharper disable StringLiteralTypo

namespace SmartImage.Lib.Engines.Search;

#nullable disable

public sealed class IqdbEngine : ClientSearchEngine
{
	public IqdbEngine() : base("https://iqdb.org/?url=", "https://iqdb.org/") { }

	public override SearchEngineOptions EngineOption => SearchEngineOptions.Iqdb;

	private static SearchResultItem ParseResult(IHtmlCollection<IElement> tr, SearchResult r)
	{
		var caption = tr[0];
		var img     = tr[1];
		var src     = tr[2];

		string url = null!;

		//img.ChildNodes[0].ChildNodes[0].TryGetAttribute("href")

		try {
			//url = src.FirstChild.ChildNodes[2].ChildNodes[0].TryGetAttribute("href");

			url = img.ChildNodes[0].ChildNodes[0].TryGetAttribute("href");

			// Links must begin with http:// in order to work with "start"

		}
		catch {
			// ignored
		}

		int w = 0, h = 0;

		if (tr.Length >= 4) {
			var res = tr[3];

			string[] wh = res.TextContent.Split(Strings.Constants.MUL_SIGN);

			string wStr = wh[0].SelectOnlyDigits();
			w = Int32.Parse(wStr);

			// May have NSFW caption, so remove it

			string hStr = wh[1].SelectOnlyDigits();
			h = Int32.Parse(hStr);
		}

		double? sim;

		if (tr.Length >= 5) {
			var    simNode = tr[4];
			string simStr  = simNode.TextContent.Split('%')[0];
			sim = Double.Parse(simStr);
			sim = Math.Round(sim.Value, 2);
		}
		else {
			sim = null;
		}

		Url uri;

		if (url != null) {
			if (url.StartsWith("//")) {
				url = "http:" + url;
			}

			uri = url;
		}
		else {
			uri = null;
		}

		var result = new SearchResultItem(r)
		{
			Url         = uri,
			Similarity  = sim,
			Width       = w,
			Height      = h,
			Source      = src.TextContent,
			Description = caption.TextContent,
		};

		return result;
	}

	private async Task<IDocument> GetDocumentAsync(SearchQuery query)
	{
		const int MAX_FILE_SIZE = 0x800000;

		var response = await EndpointUrl.PostMultipartAsync(m =>
		{
			m.AddString("MAX_FILE_SIZE", MAX_FILE_SIZE.ToString());
			m.AddString("url", query.IsUrl ? query.Value : String.Empty);

			if (query.IsUrl) { }
			else if (query.IsFile) {
				m.AddFile("file", query.Value, fileName: "image.jpg");
			}

			return;
		});

		var s = await response.GetStringAsync();

		var parser = new HtmlParser();
		return await parser.ParseDocumentAsync(s);
	}

	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken? token = null)
	{
		// Don't select other results

		var sr  = await base.GetResultAsync(query, token);
		var doc = await GetDocumentAsync(query);

		if (doc == null) {
			sr.ErrorMessage = $"Could not retrieve data";
			return sr;
		}

		Trace.Assert(doc != null);

		var pages  = doc.Body.SelectSingleNode("//div[@id='pages']");
		var tables = ((IHtmlElement) pages).SelectNodes("div/table");

		// No relevant results?

		var ns = doc.Body.QuerySelector("#pages > div.nomatch");

		if (ns != null) {

			sr.Status = SearchResultStatus.NoResults;
			goto ret;
		}

		var select = tables.Select(table => ((IHtmlElement) table)
			                           .QuerySelectorAll("table > tbody > tr:nth-child(n)"));

		var images = select.Select(x => ParseResult(x, sr)).ToList();

		// First is original image
		images.RemoveAt(0);

		var best = images[0];
		// sr.PrimaryResult.UpdateFrom(best);
		sr.Results.AddRange(images.Skip(1));

		/*sr.Results.Quality = sr.PrimaryResult.Similarity switch
		{
			>= 75 => ResultQuality.High,
			_ or null => ResultQuality.NA,
		};*/

		ret:
		FinalizeResult(sr);
		return sr;
	}

	public override void Dispose()
	{
		base.Dispose();
	}
}