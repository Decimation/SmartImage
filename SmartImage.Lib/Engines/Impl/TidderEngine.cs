using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HtmlAgilityPack;
using SimpleCore.Net;
using SmartImage.Lib.Searching;

#nullable enable
namespace SmartImage.Lib.Engines.Impl
{
	public sealed class TidderEngine : SearchEngine
	{
		public TidderEngine() : base("http://tidder.xyz/?imagelink=") { }

		public override SearchEngineOptions Engine => SearchEngineOptions.Tidder;


		public override SearchResult GetResult(ImageQuery url)
		{
			//http://tidder.xyz/?imagelink=

			//

			var sr = base.GetResult(url);


			try {
				if (!Network.TryGetString(sr.RawUri.ToString()!, out var html)) {
					sr.RawUri            = null;
					sr.PrimaryResult.Url = null;
					//sr.AddErrorMessage("Unavailable");
					throw new Exception();
				}

				var doc = new HtmlDocument();
				doc.LoadHtml(html);

				var documentNode = doc.DocumentNode;

				var findings = documentNode.SelectNodes("//*[contains(@class, 'findings-row')]");

				if (findings == null || !findings.Any()) {
					//sr.Filter = true;
					return sr;
				}

				//Debug.WriteLine(findings.Count);

				var list = new List<ImageResult>();

				foreach (var t in findings) {
					var sub = t.SelectNodes("td");

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

					var bsr = new ImageResult()
					{
						Artist      = author,
						Description = title,
						Source      = subreddit,
						Url         = new Uri(link),
						Date        = DateTime.Parse(posted)
					};


					list.Add(bsr);
				}

				var best = list[0];

				sr.PrimaryResult.UpdateFrom(best);

				sr.OtherResults.AddRange(list);

			}
			catch (Exception e) {
				// ...
				//sr.AddErrorMessage(e.Message);
				sr.Status = ResultStatus.Failure;
			}

			return sr;
		}
	}
}