using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using HtmlAgilityPack;
using SimpleCore.Net;
using SimpleCore.Utilities;
using SmartImage.Searching;

#pragma warning disable HAA0502, HAA0601, HAA0401
#nullable enable
namespace SmartImage.Engines.Other
{
	public sealed class IqdbEngine : BaseSearchEngine
	{
		public IqdbEngine() : base("https://iqdb.org/?url=") { }

		public override string Name => "IQDB";

		public override Color Color => Color.Magenta;

		public override SearchEngineOptions Engine => SearchEngineOptions.Iqdb;

		public override float? FilterThreshold => 70.00F;

		

		private BasicSearchResult ParseResult(HtmlNodeCollection tr)
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


			var i = new BasicSearchResult(url, sim, w, h, src.InnerText, null, caption.InnerText);
			i.Filter = i.Similarity < FilterThreshold;
			return i;
		}

		[DebuggerHidden]
		public override FullSearchResult GetResult(string url)
		{
			var sr = base.GetResult(url);

			try {

				var html = Network.GetSimpleResponse(sr.RawUrl!);

				//Network.WriteResponse(html);

				var doc = new HtmlDocument();
				doc.LoadHtml(html.Content);


				//var tables = doc.DocumentNode.SelectNodes("//table");

				// Don't select other results

				var pages  = doc.DocumentNode.SelectSingleNode("//div[@id='pages']");
				var tables = pages.SelectNodes("div/table");

				// No relevant results?
				bool noMatch = pages.ChildNodes.Any(n => n.GetAttributeValue("class", null) == "nomatch");

				if (noMatch) {
					//sr.ExtendedInfo.Add("No relevant results");
					// No relevant results
					sr.Filter = true;
					return sr;
				}

				var images = tables.Select(table => table.SelectNodes("tr"))
					.Select(ParseResult)
					.Cast<ISearchResult>()
					.ToList();

				// First is original image
				images.RemoveAt(0);

				var best = images[0];
				sr.UpdateFrom(best);
				sr.AddExtendedResults(images.ToArray());
			}
			catch (Exception e) {
				// ...
				sr.AddErrorMessage(e.Message);
			}

			return sr;
		}
	}
}