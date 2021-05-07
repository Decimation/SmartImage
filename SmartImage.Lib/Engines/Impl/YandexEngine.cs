using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using HtmlAgilityPack;
using SimpleCore.Net;
using SimpleCore.Utilities;
using SmartImage.Lib.Searching;

#pragma warning disable HAA0101, HAA0601, HAA0502, HAA0401
#nullable enable

namespace SmartImage.Lib.Engines.Impl
{
	public sealed class YandexEngine : InterpretedSearchEngine
	{
		public YandexEngine() : base("https://yandex.com/images/search?rpt=imageview&url=") { }

		public override SearchEngineOptions Engine => SearchEngineOptions.Yandex;

		public override string Name => Engine.ToString();


		private static string? GetAnalysis(HtmlDocument doc)
		{
			const string TAGS_XP =
				"//a[contains(@class, 'Tags-Item') and ../../../../div[contains(@class,'CbirTags')]]/*";

			var nodes = doc.DocumentNode.SelectNodes(TAGS_XP);

			if (nodes == null || !nodes.Any()) {
				return null;
			}

			string? appearsToContain = nodes.Select(n => n.InnerText).QuickJoin();

			return appearsToContain;
		}


		private static List<ImageResult> GetOtherImages(HtmlDocument doc)
		{
			//const string TAGS_ITEM_XP = "//a[contains(@class, 'other-sites__preview-link')]";
			//const string TAGS_ITEM_XP = "//li[contains(@class, 'other-sites__item')]";


			//$x("//a[contains(@class, 'other-sites__preview-link')]")
			//$x("//li[contains(@class, 'other-sites__item')]")

			const string item = "//li[@class='other-sites__item']";

			var tagsItem = doc.DocumentNode.SelectNodes(item);

			//Debug.WriteLine($"$ {tagsItem.Count}");


			var images = new List<ImageResult>();

			foreach (var siz in tagsItem) {
				string? link = siz.FirstChild.Attributes["href"].Value;


				var resText = siz.FirstChild
					.ChildNodes[1]
					.FirstChild
					.InnerText;


				//other-sites__snippet

				var snippet = siz.ChildNodes[1];
				var title   = snippet.FirstChild;
				var site    = snippet.ChildNodes[1];
				var desc    = snippet.ChildNodes[2];

				var (w, h) = ParseResolution(resText);

				images.Add(new ImageResult()
				{
					Url         = new Uri(link),
					Site        = site.InnerText,
					Description = title.InnerText,
					Width       = w,
					Height      = h,
				});

			}

			return images;
		}

		private static (int? w, int? h) ParseResolution(string resText)
		{
			string[] resFull = resText.Split(Formatting.MUL_SIGN);

			int? w = null, h = null;

			if (resFull.Length == 1 && resFull[0] == resText) {
				const string TIMES_DELIM = "&times;";

				if (resText.Contains(TIMES_DELIM)) {
					resFull = resText.Split(TIMES_DELIM);
				}

				if (resFull.Length == 2) {
					w = Int32.Parse(resFull[0]);
					h = Int32.Parse(resFull[1]);
				}
			}


			return (w, h);
		}

		private static List<ImageResult> GetImages(HtmlDocument doc)
		{
			const string TAGS_ITEM_XP = "//a[contains(@class, 'Tags-Item')]";

			const string CBIR_ITEM = "CbirItem";


			var tagsItem = doc.DocumentNode.SelectNodes(TAGS_ITEM_XP);
			var images   = new List<ImageResult>();

			if (tagsItem.Count == 0) {
				return images;
			}

			var sizeTags = tagsItem.Where(sx =>
				!sx.ParentNode.ParentNode.Attributes["class"].Value.Contains(CBIR_ITEM));


			foreach (var siz in sizeTags) {
				string? link = siz.Attributes["href"].Value;

				string? resText = siz.FirstChild.InnerText;


				var (w, h) = ParseResolution(resText);

				if (!w.HasValue || !h.HasValue) {
					continue;
				}

				var yi = new ImageResult
				{
					Url    = new Uri(link),
					Width  = w,
					Height = h,
				};

				images.Add(yi);
			}

			return images;
		}


		protected override SearchResult Process(HtmlDocument doc, SearchResult sr)
		{
			// Automation detected
			const string AUTOMATION_ERROR_MSG = "Please confirm that you and not a robot are sending requests";


			if (doc.Text.Contains(AUTOMATION_ERROR_MSG)) {
				sr.ErrorMessage = "Yandex requests exceeded; on cooldown";
				sr.Status       = ResultStatus.Unavailable;

				Debug.WriteLine($"{Name} is on cooldown!");
				return sr;
			}


			/*
			 * Parse what the image looks like
			 */

			string? looksLike = GetAnalysis(doc);


			/*
			 * Find and sort through high resolution image matches
			 */

			var images = GetImages(doc);

			/*if (!images.Any()) {
				//sr.Filter = true;
				//return sr;

				//other-sites__preview-link

				Debug.WriteLine($"yandex other");

				images = GetOtherImages(doc);
				images = images.Take(NConsole.MAX_DISPLAY_OPTIONS).ToList();
			}*/

			var otherImages = GetOtherImages(doc);

			//Debug.WriteLine($"yandex: {images.Count} | yandex other: {otherImages.Count}");

			images.AddRange(otherImages);
			//images = images.Distinct().ToList();

			//Debug.WriteLine($"yandex total: {images.Count}");

			images = images.OrderByDescending(r => r.PixelResolution).ToList();

			//
			var best = images[0];
			sr.PrimaryResult.UpdateFrom(best);


			if (looksLike != null) {
				//sr.Metadata.Add("Analysis", looksLike);
				sr.PrimaryResult.Description = looksLike;
			}

			sr.OtherResults.AddRange(images);

			const string errorMessage = "No matching images found";

			if (doc.Text.Contains(errorMessage)) {

				sr.ErrorMessage = errorMessage;
				sr.Status       = ResultStatus.Extraneous;
			}

			return sr;
		}
	}
}