using System;
using System.Diagnostics;
using HtmlAgilityPack;
using SimpleCore.Net;
using SmartImage.Lib.Searching;

namespace SmartImage.Lib.Engines.Impl.Other
{
	public sealed class Ascii2DEngine : SearchEngine
	{
		public Ascii2DEngine() : base("https://ascii2d.net/search/url/") { }

		public override SearchEngineOptions Engine => SearchEngineOptions.Ascii2D;


		public override SearchResult GetResult(ImageQuery url)
		{
			var sr = base.GetResult(url);


			try {
				string html = Network.GetString(sr.RawUri.ToString()!);

				var doc = new HtmlDocument();
				doc.LoadHtml(html);

				// "//*[contains(@class, 'info-box')]"
				// "//*[contains(@class, 'row item-box')]"


				var nodes = doc.DocumentNode.SelectNodes("//*[contains(@class, 'info-box')]");


				foreach (var node in nodes) {

					var info = node.ChildNodes[3];

				}


			}
			catch (Exception e) {
				//sr.AddErrorMessage(e.Message);
				sr.Status = ResultStatus.Failure;
			}

			return sr;


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