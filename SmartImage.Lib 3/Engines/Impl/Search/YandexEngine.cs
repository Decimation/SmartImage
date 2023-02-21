using System.Diagnostics;
using System.Dynamic;
using AngleSharp.Dom;
using AngleSharp.XPath;
using Kantan.Monad;
using Kantan.Net.Utilities;
using Kantan.Text;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;

// ReSharper disable SuggestVarOrType_SimpleTypes

#pragma warning disable 8602

#nullable disable

namespace SmartImage.Lib.Engines.Impl.Search;

public sealed class YandexEngine : WebSearchEngine
{
	public YandexEngine() : base("https://yandex.com/images/search?rpt=imageview&url=")
	{
		Timeout = TimeSpan.FromSeconds(10);

	}

	protected override string NodesSelector => Serialization.S_Yandex_Images;

	public override SearchEngineOptions EngineOption => SearchEngineOptions.Yandex;

	private static string GetAnalysis(IDocument doc)
	{
		if (doc.Body is not { }) {
			return null;
		}

		var nodes = doc.Body.SelectNodes(Serialization.S_Yandex_Analysis);

		var nodes2 = doc.Body.QuerySelectorAll(Serialization.S_Yandex_Analysis2);

		nodes.AddRange(nodes2);

		if (!nodes.Any()) {
			return null;
		}

		string appearsToContain = nodes.Select(n => n.TextContent).QuickJoin();

		return appearsToContain;
	}

	private static IEnumerable<SearchResultItem> GetOtherImages(IDocument doc, SearchResult r)
	{
		var tagsItem = doc.Body.SelectNodes(Serialization.S_Yandex_OtherImages);

		if (tagsItem == null) {
			return Enumerable.Empty<SearchResultItem>();
		}

		SearchResultItem Parse(INode siz)
		{
			string link    = siz.FirstChild.TryGetAttribute(Serialization.Atr_href);
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
			w = int.Parse(resFull[0]);
			h = int.Parse(resFull[1]);
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
			doc = await GetDocumentAsync(url, query: query, token: token.Value);
		}
		catch (Exception e) {
			// Console.WriteLine(e);
			// throw;
			doc = null;
			Debug.WriteLine($"{Name}: {e.Message}", nameof(GetResultAsync));
		}

		if (doc is null or { Body: null }) {
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

		foreach (var node in await GetNodes(doc)) {
			var sri = await ParseNodeToItem(node, sr);

			if (sri != null) {
				sr.Results.Add(sri);
			}
		}

		var otherImages = GetOtherImages(doc, sr);
		sr.Results.AddRange(otherImages);

		var ext = ParseExternalInfo(doc, sr);
		sr.Results.AddRange(ext);

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

	/// <summary>
	/// Parses <em>sites containing information about the image</em>
	/// </summary>
	private static IEnumerable<SearchResultItem> ParseExternalInfo(IDocument doc, SearchResult r)
	{
		var items = doc.Body.SelectNodes(Serialization.S_Yandex_ExtInfo);
		var rg    = new List<SearchResultItem>(items.Count);

		foreach (INode item in items) {
			// var thumb = item.ChildNodes[0];
			var info  = item.ChildNodes[1];
			var title = info.ChildNodes[0].TextContent;
			var href  = info.ChildNodes[0].ChildNodes[0].TryGetAttribute(Serialization.Atr_href);
			var n     = item.ChildNodes[0].ChildNodes[0];
			var thumb = n.TryGetAttribute(Serialization.Atr_href);
			var res   = n.ChildNodes[1].TextContent;

			var sri = new SearchResultItem(r)
			{
				Title = title,
				Url   = href,
			};

			(sri.Width, sri.Height) = ParseResolution(res);

			sri.Metadata.thumb = thumb;

			rg.Add(sri);
		}

		return rg;
	}

	public override void Dispose() { }

	protected override async ValueTask<INode[]> GetNodes(IDocument doc)
	{
		var tagsItem = doc.Body.SelectNodes(NodesSelector);

		if (!tagsItem.Any()) {
			// return await Task.FromResult(Enumerable.Empty<INode>());
			return await Task.FromResult(tagsItem.ToArray());
			// return tagsItem;
		}

		var sizeTags = tagsItem.Where(sx => !sx.Parent.Parent.TryGetAttribute("class").Contains("CbirItem")).ToList();

		return await Task.FromResult(sizeTags.ToArray());

		// return sizeTags;
	}

	[ICBN]
	protected override ValueTask<SearchResultItem> ParseNodeToItem(INode siz, SearchResult r)
	{
		string link = siz.TryGetAttribute(Serialization.Atr_href);

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
			return ValueTask.FromResult(sri);
		}

		return ValueTask.FromResult<SearchResultItem>(null);
	}
}