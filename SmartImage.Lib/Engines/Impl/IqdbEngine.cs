// ReSharper disable UnusedMember.Global

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.XPath;
using HtmlAgilityPack;
using SimpleCore.Net;
using SimpleCore.Utilities;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib.Engines.Impl
{
	public sealed class IqdbEngine : InterpretedSearchEngine
	{
		public IqdbEngine() : base("https://iqdb.org/?url=") { }

		public override SearchEngineOptions Engine => SearchEngineOptions.Iqdb;

		public override string Name => "IQDB";

		//public static float? FilterThreshold => 70.00F;

		private static ImageResult ParseResult(IHtmlCollection<IElement> tr)
		{
			var caption = tr[0];
			var img     = tr[1];
			var src     = tr[2];

			string url = null!;

			//var urlNode = img.FirstChild.FirstChild;

			// if (urlNode.NodeName != "img") {
			// 	var origUrl = urlNode.GetAttr("href");
			//
			// 	// Links must begin with http:// in order to work with "start"
			// 	if (origUrl.StartsWith("//")) {
			// 		origUrl = "http:" + origUrl;
			// 	}
			//
			//
			// 	url = origUrl;
			// }

			//src.FirstChild.ChildNodes[2].ChildNodes[0].GetAttr("href")


			try {
				url = src.FirstChild.ChildNodes[2].ChildNodes[0].GetAttr("href");

				// Links must begin with http:// in order to work with "start"
				if (url.StartsWith("//")) {
					url = "http:" + url;
				}
			}
			catch { }


			int w = 0, h = 0;

			if (tr.Length >= 4) {
				var res = tr[3];

				var wh = res.TextContent.Split(Formatting.MUL_SIGN);

				var wStr = wh[0].SelectOnlyDigits();
				w = Int32.Parse(wStr);

				// May have NSFW caption, so remove it

				var hStr = wh[1].SelectOnlyDigits();
				h = Int32.Parse(hStr);
			}

			float? sim;

			if (tr.Length >= 5) {
				var simNode = tr[4];
				var simStr  = simNode.TextContent.Split('%')[0];
				sim = Single.Parse(simStr);
				sim = MathF.Round(sim.Value, 2);
			}
			else {
				sim = null;
			}

			Uri uri;

			if (url != null) {
				// var uriBuilder = new UriBuilder(url)
				// {
				// 	Scheme = Uri.UriSchemeHttps,
				// 	Port   = -1 // default port for scheme
				// };
				// uri = uriBuilder.Uri;
				uri = new Uri(url);
			}
			else {
				uri = null;
			}

			//var i = new BasicSearchResult(url, sim, w, h, src.InnerText, null, caption.InnerText);


			var i = new ImageResult()
			{
				Url         = uri,
				Similarity  = sim,
				Width       = w,
				Height      = h,
				Source      = src.TextContent,
				Description = caption.TextContent,
			};
			//i.Filter = i.Similarity < FilterThreshold;

			return i;
		}

		protected override SearchResult Process(IDocument doc, SearchResult sr)
		{

			//var tables = doc.DocumentNode.SelectNodes("//table");

			// Don't select other results

			var pages  = doc.Body.SelectSingleNode("//div[@id='pages']");
			var tables = ((IHtmlElement) pages).SelectNodes("div/table");

			// No relevant results?

			//bool noMatch = pages.ChildNodes.Any(n => (n).GetAttr("class") == "nomatch");
			var ns = doc.Body.QuerySelector("#pages > div.nomatch");

			if (ns != null) {
				//sr.ExtendedInfo.Add("No relevant results");

				// No relevant results


				sr.Status = ResultStatus.NoResults;

				return sr;
			}

			var select =
				tables.Select(table => ((IHtmlElement) table).QuerySelectorAll("table > tbody > tr:nth-child(n)"));


			var images = select.Select(ParseResult).ToList();


			// First is original image
			images.RemoveAt(0);

			var best = images[0];
			sr.PrimaryResult.UpdateFrom(best);
			sr.OtherResults.AddRange(images);

			return sr;
		}
	}
}