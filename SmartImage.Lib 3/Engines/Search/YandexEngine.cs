using System.Diagnostics;
using AngleSharp.Dom;
using AngleSharp.XPath;
using Kantan.Net.Utilities;
using Kantan.Text;

// ReSharper disable SuggestVarOrType_SimpleTypes

#pragma warning disable 8602

#nullable disable

namespace SmartImage.Lib.Engines.Search;

public sealed class YandexEngine : BaseSearchEngine, IWebContentEngine
{
	public YandexEngine() : base("https://yandex.com/images/search?rpt=imageview&url=")
	{
		Timeout = TimeSpan.FromSeconds(10);

	}

	public string NodesSelector => "//a[contains(@class, 'Tags-Item')]";

	public override SearchEngineOptions EngineOption => SearchEngineOptions.Yandex;

	private static string GetAnalysis(IDocument doc)
	{
		if (doc.Body is not { }) {
			return null;
		}

		var nodes = doc.Body.SelectNodes(
			"//a[contains(@class, 'Tags-Item') and ../../../../div[contains(@class,'CbirTags')]]/*");

		var nodes2 = doc.Body.QuerySelectorAll(".CbirTags > .Tags > .Tags-Wrapper > .Tags-Item");

		nodes.AddRange(nodes2);

		if (!nodes.Any()) {
			return null;
		}

		string appearsToContain = nodes.Select(n => n.TextContent).QuickJoin();

		return appearsToContain;
	}

	private static IEnumerable<SearchResultItem> GetOtherImages(IDocument doc, SearchResult r)
	{
		var tagsItem = doc.Body.SelectNodes("//li[@class='other-sites__item']");

		if (tagsItem == null) {
			return Enumerable.Empty<SearchResultItem>();
		}

		SearchResultItem Parse(INode siz)
		{
			string link    = siz.FirstChild.TryGetAttribute(Resources.Atr_href);
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
				Description = title?.TextContent,
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

	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken? token = null)
	{
		token ??= CancellationToken.None;
		// var sr = await base.GetResultAsync(query, token);

		var url = await GetRawUrlAsync(query);

		var sr = new SearchResult(this)
		{
			RawUrl = url
		};

		IDocument doc = null;

		try {
			doc = await ((IWebContentEngine) this).GetDocumentAsync(url, query: query, token: token.Value);
		}
		catch (Exception e) {
			// Console.WriteLine(e);
			// throw;
			doc = null;
			Debug.WriteLine($"{Name}: {e.Message}", nameof(GetResultAsync));
		}

		if (doc is null or {Body: null}) {
			sr.Status = SearchResultStatus.Failure;
			return sr;
		}

		// Automation detected
		const string AUTOMATION_ERROR_MSG = "Please confirm that you and not a robot are sending requests";

		if (doc.Body.TextContent.Contains(AUTOMATION_ERROR_MSG)) {
			sr.Status = SearchResultStatus.Cooldown;
			return sr;
		}

		/*
		 * Find and sort through high resolution image matches
		 */

		foreach (var node in await ((IWebContentEngine) this).GetNodes(doc)) {
			var sri = await ParseNodeToItem(node, sr);

			if (sri != null) {
				sr.Results.Add(sri);
			}
		}

		var otherImages = GetOtherImages(doc, sr);
		sr.Results.AddRange(otherImages);

		//

		/*
		 * Parse what the image looks like
		 */

		string looksLike = GetAnalysis(doc);

		if (looksLike != null) {
			sr.Overview = looksLike;
		}

		const string NO_MATCHING = "No matching images found";

		if (doc.Body.TextContent.Contains(NO_MATCHING)) {

			sr.ErrorMessage = NO_MATCHING;
			sr.Status       = SearchResultStatus.Extraneous;
		}

		sr.Update();
		return sr;
	}

	public override void Dispose() { }

	public async Task<IEnumerable<INode>> GetItems(IDocument doc)
	{
		var tagsItem = doc.Body.SelectNodes(NodesSelector);

		if (!tagsItem.Any()) {
			// return await Task.FromResult(Enumerable.Empty<INode>());
			return await Task.FromResult(tagsItem);
			// return tagsItem;
		}

		var sizeTags = tagsItem.Where(sx => !sx.Parent.Parent.TryGetAttribute("class").Contains("CbirItem")).ToList();

		return await Task.FromResult(sizeTags);

		// return sizeTags;
	}

	public Task<SearchResultItem> ParseNodeToItem(INode siz, SearchResult r)
	{
		string link = siz.TryGetAttribute(Resources.Atr_href);

		string resText = siz.FirstChild.GetExclusiveText();

		(int? w, int? h) = ParseResolution(resText!);

		if (!w.HasValue || !h.HasValue) {
			w = null;
			h = null;
			//link = null;
		}

		if (UriUtilities.IsUri(link, out var link2)) {
			var sri = new SearchResultItem(r)
			{
				Url    = link2,
				Width  = w,
				Height = h,
			};
			return Task.FromResult(sri);
		}

		return Task.FromResult<SearchResultItem>(null);
	}
}