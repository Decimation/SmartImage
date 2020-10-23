using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using SimpleCore.Net;
using SimpleCore.Utilities;
using SmartImage.Searching.Model;
using SmartImage.Utilities;

namespace SmartImage.Searching.Engines.SauceNao
{
	/// <summary>
	/// SauceNao non-API client
	/// </summary>
	public sealed class AltSauceNaoClient : BaseSauceNaoClient
	{
		
		private static string FindCreator(HtmlNode resultcontent)
		{
			var resulttitle = resultcontent.ChildNodes[0];
			var rti = resulttitle?.InnerText;


			var resultcontentcolumn = resultcontent.ChildNodes[1];
			var rcci = resultcontentcolumn?.InnerText;

			var i = rti ?? rcci;

			return i;

		}


		private static IEnumerable<SauceNaoSimpleResult> ParseResults(HtmlDocument doc)
		{
			// todo: improve

			var results = doc.DocumentNode.SelectNodes("//div[@class='result']");

			var images = new List<SauceNaoSimpleResult>();

			foreach (var result in results) {
				if (result.GetAttributeValue("id", string.Empty) == "result-hidden-notification") {
					continue;
				}

				var n = result.FirstChild.FirstChild;

				//var resulttableimage = n.ChildNodes[0];
				var resulttablecontent = n.ChildNodes[1];

				var resultmatchinfo = resulttablecontent.FirstChild;
				var resultsimilarityinfo = resultmatchinfo.FirstChild;

				// Contains links
				var resultmiscinfo = resultmatchinfo.ChildNodes[1];

				var links1 = resultmiscinfo.SelectNodes("a/@href");
				var link1 = links1?[0].GetAttributeValue("href", null);


				var resultcontent = resulttablecontent.ChildNodes[1];

				//var resulttitle = resultcontent.ChildNodes[0];

				var resultcontentcolumn = resultcontent.ChildNodes[1];

				// Other way of getting links
				var links2 = resultcontentcolumn.SelectNodes("a/@href");
				var link2 = links2?[0].GetAttributeValue("href", null);

				var link = link1 ?? link2;

				var title = FindCreator(resultcontent);
				var similarity = float.Parse(resultsimilarityinfo.InnerText.Replace("%", String.Empty));


				var i = new SauceNaoSimpleResult(title, link!, similarity);
				images.Add(i);
			}

			return images;
		}

		public override SearchResult GetResult(string url)
		{
			SearchResult sr = base.GetResult(url);

			var resUrl = BASIC_RESULT + url;


			try {
				var sz = Network.GetString(resUrl);
				var doc = new HtmlDocument();
				doc.LoadHtml(sz);

				var img = ParseResults(doc);

				var best = img.OrderByDescending(i => i.Similarity).First(i => i.Url != null);


				sr = new SearchResult(this, best.Url, best.Similarity);

				if (best.Caption != null) {
					sr.ExtendedInfo.Add(best.Caption);
				}


			}
			catch (Exception) {
				sr = new SearchResult(this, resUrl);
				sr.ExtendedInfo.Add("Error parsing");
			}
			finally {
				sr!.ExtendedInfo.Add("Non-API");
			}


			return sr;
		}
	}
}