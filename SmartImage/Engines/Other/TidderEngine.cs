using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using SimpleCore.Net;
using SmartImage.Searching;

#nullable enable
namespace SmartImage.Engines.Other
{
	public sealed class TidderEngine : BaseSearchEngine
	{
		public TidderEngine() : base("http://tidder.xyz/?imagelink=") { }

		public override SearchEngineOptions Engine => SearchEngineOptions.Tidder;

		public override string Name => "Tidder";

		public override Color Color => Color.Orange;

		public override FullSearchResult GetResult(string url)
		{
			//http://tidder.xyz/?imagelink=

			//

			var sr = base.GetResult(url);

			Debug.WriteLine(sr.RawUrl!);

			try {

				string? html = Network.GetString(sr.RawUrl!);
				var     doc  = new HtmlDocument();
				doc.LoadHtml(html);

				var documentNode = doc.DocumentNode;

				var findings = documentNode.SelectNodes("//*[contains(@class, 'findings-row')]");

				if (findings == null || !findings.Any()) {
					return sr;
				}
				Debug.WriteLine(findings.Count);

				var list = new List<ISearchResult>();

				for (int i = 0; i < findings.Count; i++) {


					var sub = findings[i].SelectNodes("td");

					var imgNode       = sub[0];
					var distNode      = sub[1];
					var scoreNode     = sub[2];
					var postedNode    = sub[3];
					var titleNode     = sub[4];
					var authorNode    = sub[5];
					var subredditNode = sub[6];


					string? dist      = distNode.InnerText;
					string? score     = scoreNode.InnerText;
					string? posted    = postedNode.InnerText;
					string? title     = titleNode.InnerText;
					string? author    = authorNode.InnerText;
					string? subreddit = subredditNode.InnerText;


					string link = titleNode.FirstChild.Attributes["href"].DeEntitizeValue;

					var bsr = new BasicSearchResult
					{
						Artist      = author,
						Description = title,
						Source      = subreddit,
						Url = link,
					};


					list.Add(bsr);


					Debug.WriteLine($"{i}: {sub.Count} {dist} {score} {posted} {title} {author} {subreddit} --> {link}");
				}

				var best = list[0];

				sr.UpdateFrom(best);

				sr.AddExtendedResults(list.ToArray());

			}
			catch (Exception e) {
				Debug.WriteLine(e.Message);

			}

			return sr;
		}

		public override string GetRawResultUrl(string url)
		{
			return base.GetRawResultUrl(url);
		}
	}
}