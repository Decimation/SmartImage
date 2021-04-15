using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using SimpleCore.Net;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib.Engines.Impl
{
	public sealed class Ascii2DEngine : InterpretedSearchEngine
	{
		public Ascii2DEngine() : base("https://ascii2d.net/search/url/") { }

		public override SearchEngineOptions Engine => SearchEngineOptions.Ascii2D;

		public override string Name => Engine.ToString();

		/*
		 * 
		 *
		 * color https://ascii2d.net/search/color/<hash>
		 *
		 * detail https://ascii2d.net/search/bovw/<hash>
		 *
		 */

		protected override HtmlDocument GetDocument(SearchResult sr)
		{
			var url = sr.RawUri.ToString();

			var res = Network.GetSimpleResponse(url);

			// Get redirect url (color url)
			var newUrl = res.ResponseUri.ToString();

			// https://ascii2d.net/search/color/<hash>

			// Convert to detail url

			var detailUrl = newUrl.Replace("/color/", "/bovw/");

			//Debug.WriteLine($"{url} -> {newUrl} --> {detailUrl}");

			sr.RawUri = new Uri(detailUrl);

			return base.GetDocument(sr);
		}

		//[DebuggerHidden]
		protected override SearchResult Process(HtmlDocument doc, SearchResult sr)
		{

			var nodes = doc.DocumentNode.SelectNodes("//*[contains(@class, 'info-box')]");

			var rg = new List<ImageResult>();

			foreach (var node in nodes) {

				var ir = new ImageResult();

				var info = node.ChildNodes.Where(n => !string.IsNullOrWhiteSpace(n.InnerText)).ToArray();


				var hash = info.First().InnerText;

				ir.OtherMetadata.Add("Hash", hash);

				var data = info[1].InnerText.Split(' ');

				var res = data[0].Split('x');
				ir.Width  = int.Parse(res[0]);
				ir.Height = int.Parse(res[1]);

				var fmt = data[1];

				var size = data[2];

				if (info.Length >= 3) {
					var node2 = info[2];
					var desc  = info.Last().FirstChild;
					var ns    = desc.NextSibling;

					if (node2.ChildNodes.Count >= 2 && node2.ChildNodes[1].ChildNodes.Count >= 2) {
						var node2Sub = node2.ChildNodes[1];

						if (node2Sub.ChildNodes.Count >= 8) {
							ir.Description = node2Sub.ChildNodes[3].InnerText.Trim();
							ir.Artist      = node2Sub.ChildNodes[5].InnerText.Trim();
							ir.Site        = node2Sub.ChildNodes[7].InnerText.Trim();

						}
					}

					//var childNode = ns.ChildNodes[1].ChildNodes[0];
					if (ns.ChildNodes.Count >= 4) {
						var childNode = ns.ChildNodes[3];
						//Debug.WriteLine($"{childNode.Attributes.Select(a=>a.Name + $" {a.Value}").QuickJoin()}");


						var l1 = childNode.GetAttributeValue("href", null);

						if (l1 is not null) {
							ir.Url = new Uri(l1);

						}

						//info[2].ChildNodes[1].ChildNodes[3]
						//info[2].ChildNodes[1].ChildNodes[5]
						//info[2].ChildNodes[1].ChildNodes[7]
					}
				}

				rg.Add(ir);
			}

			// Skip original image

			rg = rg.Skip(1).ToList();

			sr.PrimaryResult = rg.First();

			//sr.PrimaryResult.UpdateFrom(rg[0]);

			sr.OtherResults.AddRange(rg);


			return sr;
		}
	}
}