using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using HtmlAgilityPack;
using SimpleCore.Net;
using SimpleCore.Utilities;
using SmartImage.Searching.Model;

#pragma warning disable HAA0101, HAA0601, HAA0502, HAA0401
#nullable enable

namespace SmartImage.Searching.Engines.Other
{
	public sealed class YandexEngine : BasicSearchEngine
	{
		public YandexEngine() : base("https://yandex.com/images/search?rpt=imageview&url=") { }

		public override SearchEngineOptions Engine => SearchEngineOptions.Yandex;

		public override string Name => "Yandex";

		private struct YandexResult : ISearchResult
		{
			public float? Similarity { get; set; }

			public int? Width { get; set; }

			public int? Height { get; set; }

			public string? Caption { get; set; }

			public string Url { get; set; }


			internal YandexResult(int width, int height, string url)
			{
				Width = width;
				Height = height;
				Url = url;
				Caption = null;
				Similarity = null;
			}

			public override string ToString()
			{
				return String.Format("{0}x{1} {2} [{3:N}]", Width, Height, Url, ((ISearchResult) this).FullResolution);
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
			var cbirItemNode = doc.DocumentNode.SelectNodes("//div[contains(@class, 'CbirItem')]")
				.First(nd =>
					nd.ChildNodes.Any(g => g.InnerText.Contains("looks like a picture")));


			var looksLikeTagsNode = cbirItemNode.ChildNodes[1].ChildNodes;

			var looksLikeInnerText = looksLikeTagsNode.Select(p => p.InnerText);


			var looksLike = String.Format("Looks like: {0}", looksLikeInnerText.QuickJoin());


			return looksLike;
		}

		private static List<ISearchResult> GetYandexImages(HtmlDocument doc)
		{
			var tagsItem = doc.DocumentNode.SelectNodes("//a[contains(@class, 'Tags-Item')]");

			var sizeTags = tagsItem.Where(sx =>
				!sx.ParentNode.ParentNode.Attributes["class"].Value.Contains("CbirItem"));


			var images = new List<ISearchResult>();

			foreach (var siz in sizeTags) {
				var link = siz.Attributes["href"].Value;

				var resText = siz.FirstChild.InnerText;
				var resFull = resText.Split('×');
				var w = Int32.Parse(resFull[0]);
				var h = Int32.Parse(resFull[1]);
				var totalRes = w * h;

				if (totalRes >= TOTAL_RES_MIN) {
					var restRes = Network.GetSimpleResponse(link);

					if (restRes.StatusCode != HttpStatusCode.NotFound) {
						var yi = new YandexResult(w, h, link);

						images.Add(yi);
					}
				}


				// todo
			}

			return images;
		}

		public override FullSearchResult GetResult(string url)
		{
			// todo: slow

			var sr = base.GetResult(url);
			
			try {

				// Get more info from Yandex

				var html = Network.GetString(sr.RawUrl);
				var doc = new HtmlDocument();
				doc.LoadHtml(html);

				/*
				 * Parse what the image looks like
				 */

				var looksLike = GetYandexAnalysis(doc);
				sr.Caption = looksLike;


				/*
				 * Find and sort through high resolution image matches
				 */

				var images = GetYandexImages(doc);

				ISearchResult[] bestImages = FilterAndSelectBestImages(images);

				//
				var best = images[0];
				sr.Width = best.Width;
				sr.Height = best.Height;
				sr.Url = best.Url;


				sr.AddExtendedResults(bestImages);
			}
			catch (Exception) {
				// ...

				sr.ExtendedInfo.Add("Error parsing");
			}


			return sr;
		}

		public override Color Color => Color.Khaki;
	}
}