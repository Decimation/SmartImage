using System;
using System.Collections.Generic;
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
	public sealed class IqdbEngine : BasicSearchEngine
	{
		public IqdbEngine() : base("https://iqdb.org/?url=") { }

		public override string Name => "IQDB";

		public override Color Color => Color.Pink;

		public override SearchEngineOptions Engine => SearchEngineOptions.Iqdb;

		private struct IqdbResult : ISearchResult
		{
			public string? Caption { get; set; }

			public string Source { get; }

			public int? Width { get; set; }

			public int? Height { get; set; }

			public string Url { get; set; }

			public float? Similarity { get; set; }

			public IqdbResult(string caption, string source, string url, int width, int height, float? similarity)
			{
				Caption = caption;
				Url = url;
				Source = source;
				Width = width;
				Height = height;
				Similarity = similarity;
			}

			public override string ToString()
			{
				return
					$"{nameof(Caption)}: {Caption}, {nameof(Source)}: {Source}, {nameof(Width)}: {Width}, {nameof(Height)}: {Height}, {nameof(Url)}: {Url}, {nameof(Similarity)}: {Similarity}";
			}
		}

		private static IqdbResult ParseResult(HtmlNodeCollection tr)
		{
			var caption = tr[0];
			var img = tr[1];
			var src = tr[2];

			string url = null!;

			var urlNode = img.FirstChild.FirstChild;

			if (urlNode.Name != "img") {
				url = "http:" + urlNode.Attributes["href"].Value;
			}

			int w = 0, h = 0;

			if (tr.Count >= 4) {
				var res = tr[3];
				var wh = res.InnerText.Split("×");

				var wStr = wh[0].SelectOnlyDigits();
				w = int.Parse(wStr);

				// May have NSFW caption, so remove it

				var hStr = wh[1].SelectOnlyDigits();
				h = int.Parse(hStr);
			}

			float? sim;

			if (tr.Count >= 5) {
				var simNode = tr[4];
				var simStr = simNode.InnerText.Split('%')[0];
				sim = float.Parse(simStr);
			}
			else {
				sim = null;
			}


			var i = new IqdbResult(caption.InnerText, src.InnerText, url, w, h, sim);

			return i;
		}

		public override FullSearchResult GetResult(string url)
		{
			var sr = base.GetResult(url);

			try {
				
				var html = Network.GetSimpleResponse(sr.RawUrl);

				//Network.WriteResponse(html);

				var doc = new HtmlDocument();
				doc.LoadHtml(html.Content);
				

				//var tables = doc.DocumentNode.SelectNodes("//table");

				// Don't select other results

				var pages = doc.DocumentNode.SelectSingleNode("//div[@id='pages']");
				var tables = pages.SelectNodes("div/table");

				// No relevant results?
				bool noMatch = pages.ChildNodes.Any(n => n.GetAttributeValue("class", null) == "nomatch");

				if (noMatch) {
					sr.ExtendedInfo.Add("No relevant results");
					return sr;
				}

				var images = new List<ISearchResult>();

				foreach (var table in tables) {

					var tr = table.SelectNodes("tr");

					var i = ParseResult(tr);

					images.Add(i);
				}

				// First is original image
				images.RemoveAt(0);

				var best = images[0];
				sr.Url = best.Url;
				sr.Similarity = best.Similarity;

				sr.AddExtendedResults(images.ToArray());


			}
			catch (Exception) {
				// ...

				sr.ExtendedInfo.Add("Error parsing");
			}

			return sr;
		}
	}
}