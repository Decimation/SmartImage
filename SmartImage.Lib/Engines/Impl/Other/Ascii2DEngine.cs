using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using AngleSharp;
using HtmlAgilityPack;
using SimpleCore.Net;
using SimpleCore.Utilities;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib.Engines.Impl.Other
{
	public sealed class Ascii2DEngine : InterpretedSearchEngine
	{
		public Ascii2DEngine() : base("https://ascii2d.net/search/url/") { }

		public override SearchEngineOptions Engine => SearchEngineOptions.Ascii2D;

		public override string Name => Engine.ToString();

		/*
		 * todo:
		 *
		 * color https://ascii2d.net/search/color/<hash>
		 *
		 * detail https://ascii2d.net/search/bovw/<hash>
		 *
		 */

		//[DebuggerHidden]
		protected override SearchResult Process(HtmlDocument doc, SearchResult sr)
		{
			
			var nodes    = doc.DocumentNode.SelectNodes("//*[contains(@class, 'info-box')]");

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

			sr.OtherResults.AddRange(rg);
			

			return sr;
		}
	}
}