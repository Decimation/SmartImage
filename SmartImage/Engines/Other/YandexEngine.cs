using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using HtmlAgilityPack;
using SimpleCore.Net;
using SimpleCore.Utilities;
using SmartImage.Searching;

#pragma warning disable HAA0101, HAA0601, HAA0502, HAA0401
#nullable enable

namespace SmartImage.Engines.Other
{
	public sealed class YandexEngine : BasicSearchEngine
	{
		public YandexEngine() : base("https://yandex.com/images/search?rpt=imageview&url=") { }

		public override SearchEngineOptions Engine => SearchEngineOptions.Yandex;

		public override string Name => "Yandex";

		private struct YandexResult : ISearchResult
		{
			public bool   Filter     { get; set; }
			public float? Similarity { get; set; }

			public int? Width { get; set; }

			public int? Height { get; set; }

			public string? Caption { get; set; }

			public string Url { get; set; }


			internal YandexResult(int width, int height, string url)
			{
				Width      = width;
				Height     = height;
				Url        = url;
				Caption    = null;
				Similarity = null;
				Filter     = false;
			}

			public override string ToString()
			{
				return $"{Width}x{Height} {Url} [{((ISearchResult) this).FullResolution:N}]";
			}
		}

		private const int TOTAL_RES_MIN = 500_000;

		private static ISearchResult[] FilterAndSelectBestImages(List<ISearchResult> rg)
		{
			const int TAKE_N = 5;

			var best = rg.OrderByDescending(i => i.FullResolution)
				.Take(TAKE_N)
				.ToArray();

			return best;
		}

		private static string GetYandexAnalysis(HtmlDocument doc)
		{
			const string TAGS_XP = "//div[contains(@class, 'Tags_type_simple')]/*";

			var nodes = doc.DocumentNode.SelectNodes(TAGS_XP);

			string? appearsToContain = nodes.Select(n => n.InnerText).QuickJoin();

			return appearsToContain;
		}


		private static List<ISearchResult> GetYandexImages(HtmlDocument doc)
		{
			const string TAGS_ITEM_XP = "//a[contains(@class, 'Tags-Item')]";

			const string CBIR_ITEM = "CbirItem";


			var tagsItem = doc.DocumentNode.SelectNodes(TAGS_ITEM_XP);

			var sizeTags = tagsItem.Where(sx =>
				!sx.ParentNode.ParentNode.Attributes["class"].Value.Contains(CBIR_ITEM));

			

			var images = new List<ISearchResult>();

			foreach (var siz in sizeTags) {
				string? link = siz.Attributes["href"].Value;

				string? resText = siz.FirstChild.InnerText;

				string[]? resFull  = resText.Split(Formatting.MUL_SIGN);
				
				int       w        = Int32.Parse(resFull[0]);
				int       h        = Int32.Parse(resFull[1]);
				int       totalRes = w * h;

				if (totalRes >= TOTAL_RES_MIN) {
					var restRes = Network.GetSimpleResponse(link);

					if (restRes.StatusCode != HttpStatusCode.NotFound) {
						var yi = new YandexResult(w, h, link);

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

				string? html = Network.GetString(sr.RawUrl);
				var     doc  = new HtmlDocument();
				doc.LoadHtml(html);

				/*
				 * Parse what the image looks like
				 */

				string? looksLike = GetYandexAnalysis(doc);
				sr.Caption = looksLike;


				/*
				 * Find and sort through high resolution image matches
				 */

				var images = GetYandexImages(doc);

				ISearchResult[] bestImages = FilterAndSelectBestImages(images);

				//
				var best = images[0];
				sr.Width  = best.Width;
				sr.Height = best.Height;
				sr.Url    = best.Url;


				sr.AddExtendedResults(bestImages);
			}
			catch (Exception e) {
				// ...

				sr.ExtendedInfo.Add($"Error parsing: {e.Message}");
			}


			return sr;
		}

		public override Color Color => Color.Khaki;
	}
}