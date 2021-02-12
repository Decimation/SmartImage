using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.XPath;
using HtmlAgilityPack;
using SimpleCore.Net;
using SimpleCore.Utilities;
using SmartImage.Core;
using SmartImage.Searching;

#pragma warning disable HAA0101, HAA0601, HAA0502, HAA0401
#nullable enable

namespace SmartImage.Engines.Other
{
	public sealed class YandexEngine : BaseSearchEngine
	{
		public YandexEngine() : base("https://yandex.com/images/search?rpt=imageview&url=") { }

		public override SearchEngineOptions Engine => SearchEngineOptions.Yandex;

		public override string Name => "Yandex";


		private const int TOTAL_RES_MIN = 500_000;

		private static string? GetAnalysis(HtmlDocument doc)
		{
			const string TAGS_XP = "//div[contains(@class, 'Tags_type_simple')]/*";

			var nodes = doc.DocumentNode.SelectNodes(TAGS_XP);

			if (nodes == null || !nodes.Any()) {
				return null;
			}

			string? appearsToContain = nodes.Select(n => n.InnerText).QuickJoin();

			return appearsToContain;
		}


		private static List<BasicSearchResult> GetImages(HtmlDocument doc)
		{
			const string TAGS_ITEM_XP = "//a[contains(@class, 'Tags-Item')]";

			const string CBIR_ITEM = "CbirItem";


			var tagsItem = doc.DocumentNode.SelectNodes(TAGS_ITEM_XP);

			var sizeTags = tagsItem.Where(sx =>
				!sx.ParentNode.ParentNode.Attributes["class"].Value.Contains(CBIR_ITEM));


			var images = new List<BasicSearchResult>();

			foreach (var siz in sizeTags) {
				string? link = siz.Attributes["href"].Value;

				string? resText = siz.FirstChild.InnerText;

				string[]? resFull = resText.Split(Formatting.MUL_SIGN);

				int w        = Int32.Parse(resFull[0]);
				int h        = Int32.Parse(resFull[1]);
				int totalRes = w * h;

				if (totalRes >= TOTAL_RES_MIN) {
					var restRes = Network.GetSimpleResponse(link);

					if (restRes.StatusCode != HttpStatusCode.NotFound) {
						var yi = new BasicSearchResult(link, w, h);

						images.Add(yi);
					}
				}
			}

			return images;
		}


		public override FullSearchResult GetResult(string url)
		{
			// todo: slow

			var sr = base.GetResult(url);

			try {

				// Get more info from Yandex

				string? html = Network.GetString(sr.RawUrl!);
				var     doc  = new HtmlDocument();
				doc.LoadHtml(html);

				/*
				 * Parse what the image looks like
				 */

				string? looksLike = GetAnalysis(doc);



				/*
				 * Find and sort through high resolution image matches
				 */

				var images = GetImages(doc);

				if (!images.Any()) {
					sr.Filter = true;
					return sr;
				}

				Debug.WriteLine($"{Name} has {images.Count} results");
				ISearchResult[] bestImages = FullSearchResult.FilterAndSelectBestImages(images);

				//
				var best = images[0];
				sr.UpdateFrom(best);

				if (looksLike!=null) {
					sr.Description = looksLike;
				}
				

				sr.AddExtendedResults(bestImages);

			}
			catch (Exception e) {
				// ...
				sr.AddErrorMessage(e.Message);
			}


			return sr;
		}

		public override Color Color => Color.Khaki;
	}
}