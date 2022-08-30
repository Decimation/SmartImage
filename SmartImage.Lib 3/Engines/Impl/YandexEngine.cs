using AngleSharp.Dom;
using AngleSharp.XPath;
using Kantan.Net.Utilities;
using Kantan.Text;

// ReSharper disable SuggestVarOrType_SimpleTypes

#pragma warning disable 8602

#nullable enable

namespace SmartImage_3.Lib.Engines.Impl;

public sealed class YandexEngine : WebContentSearchEngine
{
	public YandexEngine() : base("https://yandex.com/images/search?rpt=imageview&url=") { }

	public override SearchEngineOptions EngineOption => SearchEngineOptions.Yandex;

	private static string? GetAnalysis(IDocument doc)
	{
		var nodes = doc.Body.SelectNodes("//a[contains(@class, 'Tags-Item') and " +
		                                 "../../../../div[contains(@class,'CbirTags')]]/*");

		var nodes2 = doc.Body.QuerySelectorAll(".CbirTags > .Tags > " +
		                                       ".Tags-Wrapper > .Tags-Item");

		nodes.AddRange(nodes2);

		if (!nodes.Any()) {
			return null;
		}

		string? appearsToContain = nodes.Select(n => n.TextContent).QuickJoin();

		return appearsToContain;
	}

	private static List<SearchResultItem>? GetOtherImages(IDocument doc, SearchResult r)
	{
		var tagsItem = doc.Body.SelectNodes("//li[@class='other-sites__item']");

		if (tagsItem == null) {
			return null;
		}

		SearchResultItem Parse(INode siz)
		{
			string link    = siz.FirstChild.TryGetAttribute("href");
			string resText = siz.FirstChild.ChildNodes[1].FirstChild.TextContent;

			//other-sites__snippet

			var snippet = siz.ChildNodes[1];
			var title   = snippet.FirstChild;
			var site    = snippet.ChildNodes[1];
			var desc    = snippet.ChildNodes[2];

			var (w, h) = ParseResolution(resText);

			return new SearchResultItem(r)
			{
				Url         = new Uri(link),
				Site        = site.TextContent,
				Description = title.TextContent,
				Width       = w,
				Height      = h,
			};
		}

		return tagsItem.AsParallel().Select(Parse).ToList();
	}

	private static (int? w, int? h) ParseResolution(string resText)
	{
		string[] resFull = resText.Split(Strings.Constants.MUL_SIGN);

		int? w = null, h = null;

		if (resFull.Length == 1 && resFull[0] == resText) {
			const string TIMES_DELIM = "&times;";

			if (resText.Contains(TIMES_DELIM)) {
				resFull = resText.Split(TIMES_DELIM);
			}
		}

		if (resFull.Length == 2) {
			w = Int32.Parse(resFull[0]);
			h = Int32.Parse(resFull[1]);
		}

		return (w, h);
	}

	private static List<SearchResultItem> GetImages(IDocument doc, SearchResult r)
	{
		var tagsItem = doc.Body.SelectNodes("//a[contains(@class, 'Tags-Item')]");
		var images   = new List<SearchResultItem>();

		if (tagsItem.Count == 0) {
			return images;
		}

		var sizeTags = tagsItem.Where(sx => !sx.Parent.Parent.TryGetAttribute("class")
		                                       .Contains("CbirItem"));

		SearchResultItem Parse(INode siz)
		{
			string? link = siz.TryGetAttribute("href");

			string? resText = siz.FirstChild.GetExclusiveText();

			(int? w, int? h) = ParseResolution(resText!);

			if (!w.HasValue || !h.HasValue) {
				w = null;
				h = null;
				//link = null;
			}

			if (UriUtilities.IsUri(link, out var link2)) { }
			else {
				link2 = null;
			}

			var yi = new SearchResultItem(r)
			{
				Url    = link2,
				Width  = w,
				Height = h,
			};

			return yi;
		}

		images.AddRange(sizeTags.AsParallel().Select(Parse));

		return images;
	}

	public async override Task<SearchResult> GetResultAsync(SearchQuery query)
	{
		var url = await GetRawUrlAsync(query);
		var doc         = (IDocument) await ParseContent(url);
		var sr          = new SearchResult();

		// Automation detected
		const string AUTOMATION_ERROR_MSG = "Please confirm that you and not a robot are sending requests";

		if (doc.Body.TextContent.Contains(AUTOMATION_ERROR_MSG)) {
			sr.Status = SearchResultStatus.Cooldown;
			return sr;
		}

		/*
		 * Parse what the image looks like
		 */

		string? looksLike = GetAnalysis(doc);

		/*
		 * Find and sort through high resolution image matches
		 */

		var images = GetImages(doc, sr);

		var otherImages = GetOtherImages(doc, sr);

		if (otherImages != null) {
			images.AddRange(otherImages);
		}

		//

		if (images.Count > 0) {
			var best = images[0];

			sr.Results.AddRange(images);

			if (looksLike != null) {
				sr.Results[0].Description = looksLike;
			}
		}

		const string NO_MATCHING = "No matching images found";

		if (doc.Body.TextContent.Contains(NO_MATCHING)) {

			sr.ErrorMessage = NO_MATCHING;
			sr.Status       = SearchResultStatus.Extraneous;
		}

		return sr;
	}

	public override void Dispose()
	{
		
	}
}