// ReSharper disable UnusedMember.Global

using System;
using System.Diagnostics;
using System.Linq;
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

		// TODO: FIX USING IMAGE LINKS
		private static ImageResult ParseResult(HtmlNodeCollection tr)
		{
			var caption = tr[0];
			var img     = tr[1];
			var src     = tr[2];

			string url = null!;

			var urlNode = img.FirstChild.FirstChild;

			if (urlNode.Name != "img") {
				var origUrl = urlNode.Attributes["href"].Value;

				// Links must begin with http:// in order to work with "start"
				if (origUrl.StartsWith("//")) {
					origUrl = "http:" + origUrl;
				}


				url = origUrl;
			}


			int w = 0, h = 0;

			if (tr.Count >= 4) {
				var res = tr[3];

				var wh = res.InnerText.Split(Formatting.MUL_SIGN);

				var wStr = wh[0].SelectOnlyDigits();
				w = Int32.Parse(wStr);

				// May have NSFW caption, so remove it

				var hStr = wh[1].SelectOnlyDigits();
				h = Int32.Parse(hStr);
			}

			float? sim;

			if (tr.Count >= 5) {
				var simNode = tr[4];
				var simStr  = simNode.InnerText.Split('%')[0];
				sim = Single.Parse(simStr);
			}
			else {
				sim = null;
			}


			//var i = new BasicSearchResult(url, sim, w, h, src.InnerText, null, caption.InnerText);
			var i = new ImageResult()
			{
				Url         = url is null ? null : new Uri(url!),
				Similarity  = sim,
				Width       = w,
				Height      = h,
				Source      = src.InnerText,
				Description = caption.InnerText,
			};
			//i.Filter = i.Similarity < FilterThreshold;
			return i;
		}

		protected override SearchResult Process(HtmlDocument doc, SearchResult sr)
		{
			//var tables = doc.DocumentNode.SelectNodes("//table");

			// Don't select other results

			var pages  = doc.DocumentNode.SelectSingleNode("//div[@id='pages']");
			var tables = pages.SelectNodes("div/table");

			// No relevant results?

			bool noMatch = pages.ChildNodes.Any(n => n.GetAttributeValue("class", null) == "nomatch");

			if (noMatch) {
				//sr.ExtendedInfo.Add("No relevant results");
				
				// No relevant results
				

				sr.Status = ResultStatus.NoResults;

				return sr;
			}

			var images = tables.Select(table => table.SelectNodes("tr"))
				.Select(ParseResult)
				.Cast<ImageResult>()
				.ToList();

			// First is original image
			images.RemoveAt(0);

			var best = images[0];
			sr.PrimaryResult.UpdateFrom(best);
			sr.OtherResults.AddRange(images);

			return sr;
		}
	}
}