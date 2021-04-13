using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

		//[DebuggerHidden]
		protected override SearchResult Process(HtmlDocument doc, SearchResult sr)
		{
			//Debug.WriteLine($"{doc.Text.Contains("csrf-token")}");
			var nodes = doc.DocumentNode.SelectNodes("//*[contains(@class, 'info-box')]");

			var rg = new List<ImageResult>();

			foreach (var node in nodes) {

				var ir   = new ImageResult();

				var info = node.ChildNodes.Where(n => !string.IsNullOrWhiteSpace(n.InnerText)).ToArray();

				for (int i = 0; i < info.Length; i++) {
					var childNode = info[i];

					

					Debug.WriteLine(
						$"[{i}] [{childNode.InnerText.Trim()}] {childNode.Attributes.Select(a => a.Value).QuickJoin()}");
				}
				var hash = info.First().InnerText;
				
				var data = info[1].InnerText.Split(' ');
				
				var res  = data[0].Split('x');
				ir.Width  = int.Parse(res[0]);
				ir.Height = int.Parse(res[1]);

				var fmt = data[1];

				var size = data[2];
				if (info.Length >= 3) {
					var desc = info.Last().FirstChild;
					var ns   = desc.NextSibling;

					//var childNode = ns.ChildNodes[1].ChildNodes[0];
					if (ns.ChildNodes.Count >= 4) {
						var childNode = ns.ChildNodes[3];
						//Debug.WriteLine($"{childNode.Attributes.Select(a=>a.Name + $" {a.Value}").QuickJoin()}");

					

						var l1 = childNode.GetAttributeValue("href", null);
						if (l1 is not null) {
							ir.Url = new Uri(l1);

						}

					}
				}
				rg.Add(ir);
			}

			sr.OtherResults.AddRange(rg);
			/*try {

				var srRawUrl = sr.RawUrl!;

				Debug.WriteLine($"{srRawUrl}");

				// var req  = new RestRequest(srRawUrl);
				// req.Method = Method.GET;
				//
				// var rc   = new RestClient();
				//
				// var html = rc.Execute(req);

				var html = Network.GetString(srRawUrl);


				//Network.WriteResponse(html);

				var doc = new HtmlDocument();
				doc.LoadHtml(html);


				/* //div[contains(@class, 'atag')]
				 * //div[contains(@class, 'atag') and contains(@class ,'btag')]
				 *
				 * //*[contains(@class, 'item-box')]
				 * //*[contains(@class, 'row') and contains(@class ,'item-box')]
				 * //*[@class='row item-box']
				 #1#

				var nodes = doc.DocumentNode.SelectNodes("//*[contains(@class, 'item-box')]");

				Debug.WriteLine($"{Name}:: {nodes.Count}");

				var images = new List<BaseSearchResult>();

				foreach (var node in nodes) {
					var hashNode   = node.ChildNodes[0];
					var infoNode   = node.ChildNodes[1];
					var linksNode  = node.ChildNodes[2];
					var unusedNode = node.ChildNodes[3];

					var result = new BaseSearchResult();

					//var info=infoNode.SelectSingleNode("//*[contains(@class, 'text-muted')]");


					try {


						var detailsNode = infoNode.SelectSingleNode("//*[contains(@class, 'detail-box')]/h6")
							.ChildNodes;

						Debug.WriteLine(detailsNode.Count);


						foreach (var detailNode in detailsNode) {
							var attributeValue = detailNode.GetAttributeValue("href", null);

							if (attributeValue != null) {

								Debug.WriteLine($"href>> {attributeValue}");
								result.Url = attributeValue;
								break;
							}

						}
					}
					catch (Exception e) {
						Debug.WriteLine(e);
					}
					images.Add(result);

				}

				var best = images[0];
				sr.UpdateFrom(best);
				sr.AddExtendedResults(images);
			}
			catch (Exception e) {
				// ...
				sr.AddErrorMessage(e.Message);
			}*/

			return sr;
		}
	}
}