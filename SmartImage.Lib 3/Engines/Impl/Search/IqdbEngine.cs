// Read S SmartImage.Lib IqdbEngine.cs
// 2023-01-13 @ 11:21 PM

// ReSharper disable UnusedMember.Global

#region

using System.Diagnostics;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Flurl.Http;
using Kantan.Net.Utilities;
using Kantan.Text;
using SmartImage.Lib.Results;

#endregion

// ReSharper disable StringLiteralTypo

namespace SmartImage.Lib.Engines.Impl.Search;

#nullable disable

public sealed class IqdbEngine : BaseSearchEngine, IClientSearchEngine
{
	public string EndpointUrl => "https://iqdb.org/";

	public override SearchEngineOptions EngineOption => SearchEngineOptions.Iqdb;

	public IqdbEngine() : base("https://iqdb.org/?url=")
	{
		MaxSize = 8192 * 1024; // NOTE: assuming IQDB uses kilobytes instead of kibibytes
	}

	private static SearchResultItem ParseResult(IHtmlCollection<IElement> tr, SearchResult r)
	{
		var caption = tr[0];
		var img     = tr[1];
		var src     = tr[2];

		string url = null!;

		//img.ChildNodes[0].ChildNodes[0].TryGetAttribute("href")

		try {
			//url = src.FirstChild.ChildNodes[2].ChildNodes[0].TryGetAttribute("href");

			url = img.ChildNodes[0].ChildNodes[0].TryGetAttribute(Serialization.Atr_href);

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
			w = int.Parse(wStr);

			// May have NSFW caption, so remove it

			string hStr = wh[1].SelectOnlyDigits();
			h = int.Parse(hStr);
		}

		double? sim;

		if (tr.Length >= 5) {
			var    simNode = tr[4];
			string simStr  = simNode.TextContent.Split('%')[0];
			sim = double.Parse(simStr);
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
			m.AddString("url", query.Uni.IsUri ? query.Uni.Value.ToString() : string.Empty);

			if (query.Uni.IsUri) { }
			else if (query.Uni.IsFile) {
				m.AddFile("file", query.Uni.Value.ToString(), fileName: "image.jpg");
			}

			return;
		});

		var s = await response.GetStringAsync();

		var parser = new HtmlParser();
		return await parser.ParseDocumentAsync(s).ConfigureAwait(false);
	}

	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken? token = null)
	{
		// Don't select other results

		var sr = await base.GetResultAsync(query, token);

		if (sr.Status == SearchResultStatus.IllegalInput) {
			goto ret;
		}

		var doc = await GetDocumentAsync(query);

		if (doc == null || doc.Body == null) {
			sr.ErrorMessage = $"Could not retrieve data";
			sr.Status       = SearchResultStatus.Failure;
			goto ret;
		}
		
		if (doc.Body.TextContent.Contains("too large")) {
			sr.ErrorMessage = "Image too large";
			sr.Status       = SearchResultStatus.IllegalInput;
			goto ret;
		}

		var err = doc.Body.GetElementsByClassName("err");

		if (err.Any()) {
			var fe = err[0];
			sr.Status       = SearchResultStatus.Failure;
			sr.ErrorMessage = $"{fe.TextContent}";
			goto ret;
		}

		var pages  = doc.Body.SelectSingleNode(Serialization.S_Iqdb_Pages);
		var tables = ((IHtmlElement) pages).SelectNodes("div/table");

		// No relevant results?

		var ns = doc.Body.QuerySelector(Serialization.S_Iqdb_NoMatches);

		if (ns != null) {

			sr.Status = SearchResultStatus.NoResults;
			goto ret;
		}

		var select = tables.Select(table => ((IHtmlElement) table)
			                           .QuerySelectorAll(Serialization.S_Iqdb_Table));

		var images = select.Select(x => ParseResult(x, sr)).ToList();

		// First is original image
		images.RemoveAt(0);

		// var best = images[0];
		// sr.PrimaryResult.UpdateFrom(best);
		sr.Results.AddRange(images);

		/*sr.Results.Quality = sr.PrimaryResult.Similarity switch
		{
		    >= 75 => ResultQuality.High,
		    _ or null => ResultQuality.NA,
		};*/

		ret:
		sr.Update();
		return sr;
	}

	#region

	public override void Dispose() { }

	#endregion
}