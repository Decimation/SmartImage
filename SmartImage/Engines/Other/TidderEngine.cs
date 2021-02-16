using HtmlAgilityPack;
using SimpleCore.Net;
using SmartImage.Searching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

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



			try
			{

				string? html = Network.GetString(sr.RawUrl!);
				var doc = new HtmlDocument();
				doc.LoadHtml(html);

				var documentNode = doc.DocumentNode;

				var findings = documentNode.SelectNodes("//*[contains(@class, 'findings-row')]");

				if (findings == null || !findings.Any())
				{
					sr.Filter = true;
					return sr;
				}

				//Debug.WriteLine(findings.Count);

				var list = new List<BaseSearchResult>();
				long distl;
				for (int i = 0; i < findings.Count; i++)
				{


					var sub = findings[i].SelectNodes("td");

					var imgNode = sub[0];
					var distNode = sub[1];
					var scoreNode = sub[2];
					var postedNode = sub[3];
					var titleNode = sub[4];
					var authorNode = sub[5];
					var subredditNode = sub[6];


					string? dist = distNode.InnerText;
					string? score = scoreNode.InnerText;
					string? posted = postedNode.InnerText;
					string? title = titleNode.InnerText;
					string? author = authorNode.InnerText;
					string? subreddit = subredditNode.InnerText;

					distl = long.Parse(dist);

					string link = titleNode.FirstChild.Attributes["href"].DeEntitizeValue;

					var bsr = new BaseSearchResult
					{
						Artist = author,
						Description = title,
						Source = subreddit,
						Url = link,
						Date = DateTime.Parse(posted)
					};



					list.Add(bsr);


					Debug.WriteLine(
						$"tidder {i}: {sub.Count} {dist} {score} {posted} {title} {author} {subreddit} --> {link}");
				}

				var best = list[0];

				sr.UpdateFrom(best);

				sr.AddExtendedResults(list);

			}
			catch (Exception e)
			{
				// ...
				sr.AddErrorMessage(e.Message);
			}

			return sr;
		}
	}
}