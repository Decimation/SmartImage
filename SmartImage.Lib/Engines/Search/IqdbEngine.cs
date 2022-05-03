// ReSharper disable UnusedMember.Global

using System;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Flurl.Http;
using Kantan.Net;
using Kantan.Net.Utilities;
using Kantan.Text;
using SmartImage.Lib.Engines.Search.Base;
using SmartImage.Lib.Searching;

// ReSharper disable StringLiteralTypo

namespace SmartImage.Lib.Engines.Search;

public sealed class IqdbEngine : ClientSearchEngine
{
	public IqdbEngine() : base("https://iqdb.org/?url=", "https://iqdb.org/") { }

	public override SearchEngineOptions EngineOption => SearchEngineOptions.Iqdb;
	

	public override TimeSpan Timeout => TimeSpan.FromSeconds(4.5);


	public override EngineSearchType SearchType => EngineSearchType.Image | EngineSearchType.Metadata;

	private static ImageResult ParseResult(IHtmlCollection<IElement> tr, SearchResult r)
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

		float? sim;

		if (tr.Length >= 5) {
			var    simNode = tr[4];
			string simStr  = simNode.TextContent.Split('%')[0];
			sim = Single.Parse(simStr);
			sim = MathF.Round(sim.Value, 2);
		}
		else {
			sim = null;
		}

		Uri uri;

		if (url != null) {
			if (url.StartsWith("//")) {
				url = "http:" + url;
			}

			uri = new Uri(url);
		}
		else {
			uri = null;
		}


		var result = new ImageResult(r)
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


	private async Task<IDocument> GetDocument(ImageQuery query)
	{
		const int MAX_FILE_SIZE = 0x800000;

		var response = await EndpointUrl.PostMultipartAsync(m =>
		{
			m.AddString("MAX_FILE_SIZE", MAX_FILE_SIZE.ToString());

			m.AddString("url", query.IsUri ? query.Value : String.Empty);

			if (query.IsUri) { }
			else if (query.IsFile) {
				m.AddFile("file", query.Value, fileName: "image.jpg");
			}

			return;
		});

		var s = await response.GetStringAsync();

		var parser = new HtmlParser();
		return parser.ParseDocument(s);
	}


	protected override SearchResult Process(object obj, SearchResult sr)
	{
		// Don't select other results
		var query = (ImageQuery) obj;
		var doc   = GetDocument(query);
		doc.Wait();
		
		var pages  = doc.Result.Body.SelectSingleNode("//div[@id='pages']");
		var tables = ((IHtmlElement) pages).SelectNodes("div/table");

		// No relevant results?

		var ns = doc.Result.Body?.QuerySelector("#pages > div.nomatch");

		if (ns != null) {

			sr.Status = ResultStatus.NoResults;

			return sr;
		}

		var select = tables.Select(table => ((IHtmlElement) table)
			                           .QuerySelectorAll("table > tbody > tr:nth-child(n)"));

		var images = select.Select(x=>ParseResult(x, sr)).ToList();


		// First is original image
		images.RemoveAt(0);

		var best = images[0];
		sr.PrimaryResult.UpdateFrom(best);
		sr.OtherResults.AddRange(images.Skip(1));

		sr.PrimaryResult.Quality = sr.PrimaryResult.Similarity switch
		{
			>= 75     => ResultQuality.High,
			_ or null => ResultQuality.NA,
		};

		return sr;
	}
}