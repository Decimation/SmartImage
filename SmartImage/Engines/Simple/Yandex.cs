#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using HtmlAgilityPack;
using RestSharp;
using SmartImage.Searching;
using SmartImage.Shell;
using SmartImage.Utilities;

#endregion

namespace SmartImage.Engines.Simple
{
	public sealed class Yandex : SimpleSearchEngine
	{
		public Yandex() : base("https://yandex.com/images/search?rpt=imageview&url=") { }

		public override SearchEngines Engine => SearchEngines.Yandex;

		public override string Name => "Yandex";

		private readonly struct YandexImage
		{
			internal int Width { get; }
			internal int Height { get; }
			internal string Url { get; }

			internal int FullResolution { get; }

			internal YandexImage(int width, int height, string url)
			{
				Width = width;
				Height = height;
				FullResolution = width * height;
				Url = url;
			}

			public override string ToString()
			{
				return String.Format("{0}x{1} {2} [{3:N}]", Width, Height, Url, FullResolution);
			}
		}

		private const int TOTAL_RES_MIN = 500_000;

		private static YandexImage[] FilterAndSelectBestImages(List<YandexImage> rg)
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

		private static List<YandexImage> GetYandexImages(HtmlDocument doc)
		{
			var tagsItem = doc.DocumentNode.SelectNodes("//a[contains(@class, 'Tags-Item')]");

			var sizeTags = tagsItem.Where(sx =>
				!sx.ParentNode.ParentNode.Attributes["class"].Value.Contains("CbirItem"));


			var images = new List<YandexImage>();

			foreach (var siz in sizeTags) {
				var link = siz.Attributes["href"].Value;

				var resText = siz.FirstChild.InnerText;
				var resFull = resText.Split('×');
				var w = Int32.Parse(resFull[0]);
				var h = Int32.Parse(resFull[1]);
				var totalRes = w * h;

				if (totalRes >= TOTAL_RES_MIN) {
					var restRes = NetworkUtilities.GetSimpleResponse(link);

					if (restRes.StatusCode != HttpStatusCode.NotFound) {
						var yi = new YandexImage(w, h, link);

						images.Add(yi);
					}
				}


				// todo
			}

			return images;
		}

		public override SearchResult GetResult(string url)
		{
			// todo: slow

			var raw = GetRawResultUrl(url);
			var sr = new SearchResult(this, raw);


			try {

				// Get more info from Yandex


				var html = NetworkUtilities.GetString(raw);
				var doc = new HtmlDocument();
				doc.LoadHtml(html);

				/*
				 * Parse what the image looks like
				 */


				var looksLike = GetYandexAnalysis(doc);
				sr.ExtendedInfo.Add(looksLike);


				/*
				 * Find and sort through high resolution image matches
				 */

				var images = GetYandexImages(doc);

				var bestImages = FilterAndSelectBestImages(images);
				var bestImagesLinks = bestImages.Select(i => i.Url);

				//

				sr.ExpandedMatchResults.AddRange(bestImagesLinks);

				//

				sr.AltFunction = () =>
				{
					var rg = new SearchResult[bestImages.Length];

					for (int i = 0; i < rg.Length; i++) {
						var currentBestImg = bestImages[i];
						var link = currentBestImg.Url;

						var name = string.Format("Match result #{0}", i);

						rg[i] = new SearchResult(Color, name, link);

						rg[i].ExtendedInfo.Add(string.Format("Resolution: {0}x{1}", currentBestImg.Width, currentBestImg.Height));
					}


					Commands.HandleConsoleOptions(rg);

					return null;

				};

			}
			catch (Exception) {
				// ...

				
			}


			return sr;
		}

		public override ConsoleColor Color => ConsoleColor.DarkYellow;
	}
}