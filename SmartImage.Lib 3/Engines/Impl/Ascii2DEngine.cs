#nullable disable
using System.Diagnostics;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.XPath;
using Flurl.Http;
using Kantan.Net.Utilities;

// ReSharper disable CognitiveComplexity

// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo

namespace SmartImage_3.Lib.Engines.Impl;

public sealed class Ascii2DEngine : WebContentSearchEngine
{
	public Ascii2DEngine() : base("https://ascii2d.net/search/url/") { }

	public override SearchEngineOptions EngineOption => SearchEngineOptions.Ascii2D;

	protected override async Task<Url> GetRawUrlAsync(SearchQuery query)
	{
		var uri = await base.GetRawUrlAsync(query);

		// var       request  = WebRequest.Create(uri);
		// using var response = request.GetResponse();

		var response = await uri.WithHeaders(new { User_Agent = HttpUtilities.UserAgent }).GetAsync();

		/*
		 * URL parameters
		 *
		 * color	https://ascii2d.net/search/color/<hash>
		 * detail	https://ascii2d.net/search/bovw/<hash>
		 *
		 */

		/*
		 * With Ascii2D, two requests need to be made in order to get the detail results
		 * as the color results are returned by default
		 *
		 */

		// var    responseUri = response.ResponseUri;

		var requestUri = response.ResponseMessage?.RequestMessage?.RequestUri;

		Debug.Assert(requestUri != null);

		string detailUrl = requestUri.ToString().Replace("/color/", "/bovw/");

		return new Uri(detailUrl);

	}

	public override void Dispose()
	{
		
	}

	public override async Task<SearchResult> GetResultAsync(SearchQuery query)
	{
		var sr2 = await GetRawUrlAsync(query);

		var doc = await ParseContent(sr2);

		var nodes = doc.Body.SelectNodes("//*[contains(@class, 'info-box')]");

		var rg = new List<SearchResultItem>();

		var sr = new SearchResult();

		foreach (var node in nodes)
		{
			var ir = new SearchResultItem(sr);

			var info = node.ChildNodes.Where(n => !string.IsNullOrWhiteSpace(n.TextContent)).ToArray();

			string hash = info.First().TextContent;

			// ir.OtherMetadata.Add("Hash", hash);

			string[] data = info[1].TextContent.Split(' ');

			string[] res = data[0].Split('x');
			ir.Width = int.Parse(res[0]);
			ir.Height = int.Parse(res[1]);

			string fmt = data[1];

			string size = data[2];

			if (info.Length >= 3)
			{
				var node2 = info[2];
				var desc = info.Last().FirstChild;
				var ns = desc.NextSibling;

				if (node2.ChildNodes.Length >= 2 && node2.ChildNodes[1].ChildNodes.Length >= 2)
				{
					var node2Sub = node2.ChildNodes[1];

					if (node2Sub.ChildNodes.Length >= 8)
					{
						ir.Description = node2Sub.ChildNodes[3].TextContent.Trim();
						ir.Artist = node2Sub.ChildNodes[5].TextContent.Trim();
						ir.Site = node2Sub.ChildNodes[7].TextContent.Trim();
					}
				}

				if (ns.ChildNodes.Length >= 4)
				{
					var childNode = ns.ChildNodes[3];

					string l1 = ((IHtmlElement)childNode).GetAttribute("href");

					if (l1 is not null)
					{
						ir.Url = new Uri(l1);
					}
				}
			}

			rg.Add(ir);
		}

		// Skip original image

		rg = rg.Skip(1).ToList();

		if (!rg.Any())
		{
			goto ret;
		}

		//sr.PrimaryResult.UpdateFrom(rg[0]);

		sr.Results.AddRange(rg);

	ret:
		return sr;
	}
}