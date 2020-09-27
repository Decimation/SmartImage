#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HtmlAgilityPack;
using JetBrains.Annotations;
using SmartImage.Searching.Model;
using SmartImage.Shell;
using SmartImage.Utilities;

#endregion

namespace SmartImage.Searching.Engines.Simple
{
	public sealed class IqdbClient : SimpleSearchEngine
	{
		public IqdbClient() : base("https://iqdb.org/?url=") { }

		public override string Name => "IQDB";
		public override ConsoleColor Color => ConsoleColor.Magenta;

		public override SearchEngines Engine => SearchEngines.Iqdb;

		private struct IqdbResult : IExtendedSearchResult
		{
			[CanBeNull]
			public string Caption { get; set; }

			public string Source { get; }

			public int? Width { get; set; }

			public int? Height { get; set; }

			public string Url { get; }

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

			string url = null;

			var urlNode = img.FirstChild.FirstChild;

			if (urlNode.Name != "img") {
				url = "http:" + urlNode.Attributes["href"].Value;
			}

			int w = 0, h = 0;

			if (tr.Count >= 4) {
				var res = tr[3];
				var wh = res.InnerText.Split("×");

				var wstr = wh[0].SelectOnlyDigits();
				w = int.Parse(wstr);

				// May have NSFW caption, so remove it

				var hstr = wh[1].SelectOnlyDigits();
				h = int.Parse(hstr);
			}

			float? sim;

			if (tr.Count >= 5) {
				var simnode = tr[4];
				var simstr = simnode.InnerText.Split('%')[0];
				sim = float.Parse(simstr);
			}
			else {
				sim = null;
			}


			var i = new IqdbResult(caption.InnerText, src.InnerText, url, w, h, sim);

			return i;
		}

		public override SearchResult GetResult(string url)
		{
			var raw = GetRawResultUrl(url);
			var sr = new SearchResult(this, raw);

			try {
				var html = Network.GetSimpleResponse(raw);

				//Network.WriteResponse(html);

				var doc = new HtmlDocument();
				doc.LoadHtml(html.Content);


				//var tables = doc.DocumentNode.SelectNodes("//table");

				// Don't select other results

				var pages = doc.DocumentNode.SelectSingleNode("//div[@id='pages']");
				var tables = pages.SelectNodes("div/table");

				var images = new List<IExtendedSearchResult>();

				foreach (var table in tables) {


					var tr = table.SelectNodes("tr");


					var i = ParseResult(tr);

					images.Add(i);
				}

				// First is original image
				images.RemoveAt(0);


				sr.AddExtendedInfo(images.ToArray());

			}
			catch (Exception) {
				// ...
			}

			return sr;
		}
	}
}