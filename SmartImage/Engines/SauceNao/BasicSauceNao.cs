using System;
using System.Linq;
using HtmlAgilityPack;
using SmartImage.Searching;
using SmartImage.Utilities;

namespace SmartImage.Engines.SauceNao
{
	public sealed class BasicSauceNao : BaseSauceNao
	{
		// SimpleSearchEngine

		private const string BASIC_RESULT = "https://saucenao.com/search.php?url=";


		public override SearchResult GetResult(string url)
		{
			/*string u  = BASIC_RESULT + url;
			var    sr = new SearchResult(u, Name);
			sr.ExtendedInfo.Add("API not configured");
			return sr;*/

			SearchResult sr = null;

			var sz = WebAgent.GetString(BASIC_RESULT + url);
			HtmlDocument doc = new HtmlDocument();
			doc.LoadHtml(sz);

			// todo: for now, just return the first link found, as SN already sorts by similarity and the first link is the best result
			var links = doc.DocumentNode.SelectNodes("//*[@class='resultcontentcolumn']/a/@href");

			foreach (var link in links) {
				var lk = link.GetAttributeValue("href", null);

				if (lk != null) {
					sr = new SearchResult(lk, Name);
					break;
				}
			}

			if (sr == null) {
				string u = BASIC_RESULT + url;
				sr = new SearchResult(u, Name);
			}

			return sr;


			// TODO: finish for more refined and complete results

			/*var classToGet = "result";

			var results = doc.DocumentNode.SelectNodes("//*[@class='" + classToGet + "']");

			Console.WriteLine("nresults: "+results.Count);

			foreach (HtmlNode node in results)
			{
				//string value = node.InnerText;
				// etc...

				//var n2 = node.SelectSingleNode("//*[@id=\"middle\"]/div[3]/table/tbody/tr/td[2]/div[1]/div[1]");

				//Console.WriteLine(n2.InnerText);

				// //*[@id="middle"]/div[2]/table/tbody/tr/td[2]/div[1]
				// //*[@id="middle"]/div[3]/table/tbody/tr/td[2]/div[1] 
				// //*[@id="middle"]/div[3]/table/tbody/tr/td[2]/div[1]

				var table = node.FirstChild;
				var tbody = table.FirstChild;
				var resulttablecontent = tbody.ChildNodes[1];
				var resultmatchinfo = resulttablecontent.FirstChild;
				
				var resultsimilarityinfo = resultmatchinfo.FirstChild;

				var resultcontent = resulttablecontent.ChildNodes[1];
				var resulttitle = resultcontent.FirstChild;

				// resultcontentcolumn comes after resulttitle

				// //*[@class='resultcontentcolumn']/a

				/*var links = resultcontent.SelectNodes("//*[@class='resultcontentcolumn']/a/@href");
				var links2 = links.Where(l => l.Name != "span").ToArray();
				
				foreach (var link in links2) {
					string hrefValue = link.GetAttributeValue("href", string.Empty);
					Console.WriteLine(">>> "+hrefValue);
				}#1#

				foreach (var resultcontentChildNode in resultcontent.ChildNodes) {
					if (resultcontentChildNode.GetAttributeValue("class", String.Empty)=="resultcontentcolumn") {
						foreach (var rcccnChildNode in resultcontentChildNode.ChildNodes) {
							var href = rcccnChildNode.GetAttributeValue("href", String.Empty);

							if (!string.IsNullOrWhiteSpace(href)) {
								Console.WriteLine("!>>>>"+href);
							}
						}
					}
				}

				Console.WriteLine(resultsimilarityinfo.InnerText);
			}*/

			//Console.ReadLine();
			//var sr = new SearchResult(BASIC_RESULT+url,Name);

			//return sr;
		}
	}
}