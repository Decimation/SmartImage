using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.XPath;
using SimpleCore.Net;
using SimpleCore.Utilities;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;

#nullable enable

namespace SmartImage.Lib.Engines.Impl
{
	public sealed class YandexEngine : InterpretedSearchEngine
	{
		public YandexEngine() : base("https://yandex.com/images/search?rpt=imageview&url=") { }

		public override SearchEngineOptions EngineOption => SearchEngineOptions.Yandex;

		public override string Name => EngineOption.ToString();


		private static string? GetAnalysis(IDocument doc)
		{
			const string TAGS_XP =
				"//a[contains(@class, 'Tags-Item') and ../../../../div[contains(@class,'CbirTags')]]/*";

			var nodes = doc.Body.SelectNodes(TAGS_XP);

			if (nodes == null || !nodes.Any()) {
				return null;
			}

			string? appearsToContain = nodes.Select(n => n.TextContent).QuickJoin();

			return appearsToContain;
		}


		private static List<ImageResult>? GetOtherImages(IDocument doc)
		{
			//const string TAGS_ITEM_XP = "//a[contains(@class, 'other-sites__preview-link')]";
			//const string TAGS_ITEM_XP = "//li[contains(@class, 'other-sites__item')]";


			//$x("//a[contains(@class, 'other-sites__preview-link')]")
			//$x("//li[contains(@class, 'other-sites__item')]")

			const string item = "//li[@class='other-sites__item']";

			var tagsItem = doc.Body.SelectNodes(item);

			if (tagsItem == null) {
				return null;
			}

			static ImageResult Parse(INode siz)
			{
				string? link = siz.FirstChild.TryGetAttribute("href");

				string? resText = siz.FirstChild
				                     .ChildNodes[1]
				                     .FirstChild
				                     .TextContent;


				//other-sites__snippet

				var snippet = siz.ChildNodes[1];
				var title   = snippet.FirstChild;
				var site    = snippet.ChildNodes[1];
				var desc    = snippet.ChildNodes[2];

				var (w, h) = ParseResolution(resText);

				return new ImageResult
				{
					Url         = new Uri(link),
					Site        = site.TextContent,
					Description = title.TextContent,
					Width       = w,
					Height      = h,
				};
			}

			return tagsItem.AsParallel().Select(Parse).ToList();
		}

		private static (int? w, int? h) ParseResolution(string resText)
		{
			string[] resFull = resText.Split(StringConstants.MUL_SIGN);

			int? w = null, h = null;

			if (resFull.Length == 1 && resFull[0] == resText) {
				const string TIMES_DELIM = "&times;";

				if (resText.Contains(TIMES_DELIM)) {
					resFull = resText.Split(TIMES_DELIM);
				}


			}

			if (resFull.Length == 2) {
				w = Int32.Parse(resFull[0]);
				h = Int32.Parse(resFull[1]);
			}


			return (w, h);
		}

		private static List<ImageResult> GetImages(IDocument doc)
		{
			const string TAGS_ITEM_XP = "//a[contains(@class, 'Tags-Item')]";

			const string CBIR_ITEM = "CbirItem";


			var tagsItem = doc.Body.SelectNodes(TAGS_ITEM_XP);
			var images   = new List<ImageResult>();

			if (tagsItem.Count == 0) {
				return images;
			}

			var sizeTags = tagsItem.Where(sx => !sx.Parent.Parent.TryGetAttribute("class")
			                                       .Contains(CBIR_ITEM));

			static ImageResult Parse(INode siz)
			{
				string? link = siz.TryGetAttribute("href");

				string? resText = siz.FirstChild.GetExclusiveText();


				var (w, h) = ParseResolution(resText!);

				if (!w.HasValue || !h.HasValue) {
					w = null;
					h = null;
					//link = null;
				}

				if (Network.IsUri(link, out var link2)) { }
				else {
					link2 = null;
				}

				var yi = new ImageResult
				{
					Url    = link2,
					Width  = w,
					Height = h,
				};

				return yi;
			}


			images.AddRange(sizeTags.AsParallel().Select(Parse));

			return images;
		}


		protected override SearchResult Process(IDocument doc, SearchResult sr)
		{
			// Automation detected
			const string AUTOMATION_ERROR_MSG = "Please confirm that you and not a robot are sending requests";


			if (doc.Body.TextContent.Contains(AUTOMATION_ERROR_MSG)) {
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


			var otherImages = GetOtherImages(doc);

			if (otherImages != null) {
				images.AddRange(otherImages);
			}


			images = images.OrderByDescending(r => r.PixelResolution).ToList();

			//
			var best = images[0];
			sr.PrimaryResult.UpdateFrom(best);


			if (looksLike != null) {
				//todo
				
				sr.PrimaryResult.Description = Encoding.Unicode.GetString(
					Encoding.Convert(Encoding.UTF8, Encoding.Unicode, Encoding.UTF8.GetBytes(looksLike)));
			}

			sr.OtherResults.AddRange(images);

			const string NO_MATCHING = "No matching images found";

			if (doc.Body.TextContent.Contains(NO_MATCHING)) {

				sr.ErrorMessage = NO_MATCHING;
				sr.Status       = ResultStatus.Extraneous;
			}

			return sr;
		}
	}
}