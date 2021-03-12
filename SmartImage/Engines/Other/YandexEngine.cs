using HtmlAgilityPack;
using SimpleCore.Net;
using SimpleCore.Utilities;
using SmartImage.Searching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;

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
			const string TAGS_XP = "//a[contains(@class, 'Tags-Item') and ../../../../div[contains(@class,'CbirTags')]]/*";

			var nodes = doc.DocumentNode.SelectNodes(TAGS_XP);

			if (nodes == null || !nodes.Any()) {
				return null;
			}

			string? appearsToContain = nodes.Select(n => n.InnerText).QuickJoin();

			return appearsToContain;
		}


		private static List<BaseSearchResult> GetImages(HtmlDocument doc)
		{
			const string TAGS_ITEM_XP = "//a[contains(@class, 'Tags-Item')]";

			const string CBIR_ITEM = "CbirItem";


			var tagsItem = doc.DocumentNode.SelectNodes(TAGS_ITEM_XP);

			var sizeTags = tagsItem.Where(sx =>
				!sx.ParentNode.ParentNode.Attributes["class"].Value.Contains(CBIR_ITEM));


			var images = new List<BaseSearchResult>();

			foreach (var siz in sizeTags) {
				string? link = siz.Attributes["href"].Value;

				string? resText = siz.FirstChild.InnerText;

				Debug.WriteLine($"{resText}");

				// todo

				string[] resFull = resText.Split(Formatting.MUL_SIGN);

				if (resFull.Length==1&&resFull[0]==resText) {
					Debug.WriteLine($"Skipping {resText}");
					continue;
					
				}


				int w        = Int32.Parse(resFull[0]);
				int h        = Int32.Parse(resFull[1]);
				int totalRes = w * h;

				if (totalRes >= TOTAL_RES_MIN) {
					var restRes = Network.GetSimpleResponse(link);

					if (restRes.StatusCode != HttpStatusCode.NotFound) {
						var yi = new BaseSearchResult()
						{
							Url    = link,
							Width  = w,
							Height = h,
						};

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


				// Automation detected
				const string AUTOMATION_ERROR_MSG = "Please confirm that you and not a robot are sending requests";


				if (html.Contains(AUTOMATION_ERROR_MSG)) {
					sr.AddErrorMessage("Yandex requests exceeded; on cooldown");
					return sr;
				}

				
				var doc  = new HtmlDocument();
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


				var best1      = images.OrderByDescending(i => i.FullResolution);
				var bestImages = best1.ToList();

				//
				var best = images[0];
				sr.UpdateFrom(best);

				if (looksLike != null) {
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