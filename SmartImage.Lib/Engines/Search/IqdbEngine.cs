// Read S SmartImage.Lib IqdbEngine.cs
// 2023-01-13 @ 11:21 PM

// ReSharper disable UnusedMember.Global

using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Flurl.Http;
using Kantan.Net.Utilities;
using Kantan.Text;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Engines.Search.Base;

// ReSharper disable StringLiteralTypo

namespace SmartImage.Lib.Engines.Search;

#nullable disable

public class IqdbEngine : WebSearchEngine<IqdbItem, IEnumerable<IHtmlCollection<IElement>>>, IDisposable
{

	public override SearchEngineOptions Option => SearchEngineOptions.Iqdb;

	public virtual Url Endpoint => URL_BASE;

	public IqdbEngine() : this(URL_QUERY) { }

	// private IqdbEngine(string s) : this(s) { }

	protected IqdbEngine(string b) : base(b)
	{
		MaxLength = 8_388_608; // NOTE: assuming IQDB uses kilobytes instead of kibibytes
		Timeout = TimeSpan.FromSeconds(90);
	}

	private const string URL_BASE  = "https://iqdb.org/";
	private const string URL_QUERY = "https://iqdb.org/?url=";


	protected override async Task<IDocument> GetSourceAsync(SearchResult sr, SearchQuery query, CancellationToken token = default)
	{

		IDocument document = null;

		try {
			var response = await Client.Request(Endpoint)
				               .OnError(r =>
					               {
						               Logger.LogError(r.Exception, "{Msg}", Name);


						               // Debugger.Break();
						               r.ExceptionHandled = true;

						               // var sz = await r.Response.GetStringAsync();
					               }
				               )
				               .WithTimeout(Timeout)
				               .PostMultipartAsync(m =>
				               {
					               m.AddString("MAX_FILE_SIZE", MaxLength.ToString());

					               if (query.Source.IsUri) {
						               m.AddString("url", query.Upload.Url);
					               }
					               else if (query.Source.IsFile) {
						               m.AddFile("file", query.Source.Value, fileName: "image.jpg");
					               }
					               else { }

					               // m.AddString("url", query.Upload.Url);

					               return;
				               }, cancellationToken: token);

			if (response != null) {
				var s = await response.GetStringAsync().ConfigureAwait(false);

				var parser = new HtmlParser();
				document = await parser.ParseDocumentAsync(s, token).ConfigureAwait(false);

				// goto ret;

			}
			else {
				// Debugger.Break();
			}

			response?.Dispose();

			goto ret;
		}
		catch (Exception e) {
			Debug.WriteLine($"{e.Message}!");
			Logger.LogError(e, "{Func}: {Name}", nameof(GetSourceAsync), Name);
			goto ret;
		}

	ret:

		return document;
	}


	

	protected override ValueTask<IEnumerable<IHtmlCollection<IElement>>> ParseDataAsync(IDocument src)
	{
		var pages  = src.Body.SelectSingleNode(Serialization.S_Iqdb_Pages);
		var tables = ((IHtmlElement) pages)?.SelectNodes(Serialization.S_Iqdb_DivTable);

		if (tables is null) {
			return ValueTask.FromResult(Enumerable.Empty<IHtmlCollection<IElement>>());
		}
		var select = tables.Select(static table => ((IHtmlElement) table)
			                           .QuerySelectorAll(Serialization.S_Iqdb_Table)).Skip(1);

		return ValueTask.FromResult(select);
	}

	protected override ValueTask<IEnumerable<IqdbItem>> ParseItemsAsync(IEnumerable<IHtmlCollection<IElement>> source, SearchResult r)
	{
		var buf = new List<IqdbItem>();

		foreach (var c in source) {
			var iq = IqdbItem.ParseSource(c, r);
			buf.Add(iq);
		}

		return ValueTask.FromResult<IEnumerable<IqdbItem>>(buf);
	}

	protected override bool ValidateSource(IDocument doc)
	{
		var b = base.ValidateSource(doc);

		if (!b)
			goto ret;

		if (doc is { Body: not null }) {

			if (doc.GetElementsByClassName("err") is { Length: > 0 } err) {
				var fe = err[0];
				b = false;
				goto ret;
			}

			if (doc.QuerySelector(Serialization.S_Iqdb_NoMatches) != null) {
				b = false;
			}
		}


	ret:
		return b;
	}


	public override void Dispose()
	{
		// base.Dispose();
		GC.SuppressFinalize(this);
	}

}

public record IqdbItem : SearchResultItem, IParseableResultItem<IHtmlCollection<IElement>, IqdbItem>
{

	private IqdbItem(SearchResult r) : base(r) { }

	public static IqdbItem ParseSource(IHtmlCollection<IElement> tr, SearchResult r)
	{
		var caption = tr[0];
		var img     = tr[1];
		var src     = tr[2];

		var img2         = img.Children[0].Children[0].Children[0].Attributes["src"];
		var thumbnail    = img2 != null ? Url.Combine(r.Engine.Url.Root, img2.Value) : null;
		var thumbnail1   = img.Children[0].Children[0].Attributes["alt"];
		var thumbnailAlt = thumbnail1?.Value;

		string url = null;

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
			// Url u = url;

			if (url.StartsWith("//")) {
				url = "https:" + url;

				// url = url[2..];
			}

			uri = url;
		}
		else {
			uri = null;
		}

		var result = new IqdbItem(r)
		{
			Url            = uri,
			Similarity     = sim,
			Width          = w,
			Height         = h,
			Source         = src.TextContent,
			Description    = caption.TextContent,
			Thumbnail      = thumbnail,
			ThumbnailTitle = thumbnailAlt

		};
		result.Site ??= uri?.Host;

		// r.Results.Add(result);

		return result;
	}

}