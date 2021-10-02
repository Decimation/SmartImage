using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.XPath;
using Kantan.Net;
using SmartImage.Lib.Engines.Model;
using SmartImage.Lib.Searching;
using SmartImage.Lib.Utilities;

// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator

#nullable enable
namespace SmartImage.Lib.Engines.Impl
{
	public sealed class TidderEngine : WebSearchEngine
	{
		public TidderEngine() : base("http://tidder.xyz/?imagelink=") { }

		public override SearchEngineOptions EngineOption => SearchEngineOptions.Tidder;

		public override string Name => EngineOption.ToString();

		public override EngineSearchType SearchType => EngineSearchType.External | EngineSearchType.Metadata;

		protected override SearchResult Process(object obj, SearchResult sr)
		{
			var doc = (IDocument) obj;

			var documentNode = doc.Body;

			var findings = documentNode.SelectNodes("//*[contains(@class, 'findings-row')]");

			if (findings == null || !findings.Any()) {
				//sr.Filter = true;
				return sr;
			}

			//Debug.WriteLine(findings.Count);

			var list = new List<ImageResult>();

			foreach (var t in findings) {
				var sub = ((IHtmlElement) t).SelectNodes("td");

				//var imgNode       = sub[0];
				//var distNode      = sub[1];
				//var scoreNode     = sub[2];
				var postedNode    = sub[3];
				var titleNode     = sub[4];
				var authorNode    = sub[5];
				var subredditNode = sub[6];

				//string? dist      = distNode.InnerText;
				//string? score     = scoreNode.InnerText;
				string posted    = postedNode.TextContent;
				string title     = titleNode.TextContent;
				string author    = authorNode.TextContent;
				string subreddit = subredditNode.TextContent;

				//DeEntize!
				string link = titleNode.FirstChild.TryGetAttribute("href");

				var bsr = new ImageResult
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

			return sr;
		}
	}
}