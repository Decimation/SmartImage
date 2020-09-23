#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using HtmlAgilityPack;
using RestSharp;
using SmartImage.Searching;
using SmartImage.Utilities;

#endregion

namespace SmartImage.Engines.Simple
{
	public sealed class Yandex : SimpleSearchEngine
	{
		public Yandex() : base("https://yandex.com/images/search?rpt=imageview&url=") { }

		public override SearchEngines Engine => SearchEngines.Yandex;

		public override string Name => "Yandex";

		class YandexImage
		{
			public int Width { get; }
			public int Height { get; }
			public string Link { get; }

			public int TotalRes { get; }

			public YandexImage(int width, int height, string link)
			{
				Width = width;
				Height = height;
				TotalRes = width * height;
				this.Link = link;
			}

			public override string ToString()
			{
				return string.Format("{0}x{1} {2} [{3:N}]", Width, Height, Link, Width*Height);
			}
		}

		private static IEnumerable<string> Filter(List<YandexImage> rg)
		{
			var rg2 = rg.Where(yim => (yim.TotalRes >= 500_000)).ToList();


			var best = rg2.OrderByDescending(i => i.TotalRes).Take(5).Select(img => img.Link);


			return best;
		}

		public override SearchResult GetResult(string url)
		{
			// todo: slow

			var raw = GetRawResultUrl(url);
			var sr = new SearchResult(raw, Name);


			var html = NetworkUtilities.GetString(raw);
			var doc = new HtmlDocument();
			doc.LoadHtml(html);

			/*
			 * Parse what the image looks like
			 */

			
			var cbirItemNode = doc.DocumentNode.SelectNodes("//div[contains(@class, 'CbirItem')]").First(nd =>
				nd.ChildNodes.Any(g => g.InnerText.Contains("looks like a picture")));

			var looksLikeTagsNode = cbirItemNode.ChildNodes[1].ChildNodes;

			var looksLikeInnerText = looksLikeTagsNode.Select(p => p.InnerText);
			var looksLike = string.Format("Looks like: {0}", string.Join(", ", looksLikeInnerText));

			sr.ExtendedInfo.Add(looksLike);


			/*
			 * Find and sort through high resolution image matches
			 */

			var tagsItem = doc.DocumentNode.SelectNodes("//a[contains(@class, 'Tags-Item')]");
			
			var nsizes = tagsItem.Where(sx =>
				!sx.ParentNode.ParentNode.Attributes["class"].Value.Contains("CbirItem"));

			
			var images = new List<YandexImage>();

			foreach (var siz in nsizes) {
				var link = siz.Attributes["href"].Value;

				var resText = siz.FirstChild.InnerText;
				var resFull = resText.Split('×');
				var w = int.Parse(resFull[0]);
				var h = int.Parse(resFull[1]);

				var restClient = new RestClient();
				var restReq = new RestRequest(link);
				var restRes = restClient.Execute(restReq);

				if (restRes.StatusCode != HttpStatusCode.NotFound) {
					var yi = new YandexImage(w,h,link);
					

					images.Add(yi);
				}


				// todo
			}


			var best = Filter(images);

			//

			sr.ExtraResults.AddRange(best);

			return sr;
		}
	}
}