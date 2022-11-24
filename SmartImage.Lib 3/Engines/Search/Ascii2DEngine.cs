#nullable disable
using System.Diagnostics;
using System.Drawing;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.XPath;
using Flurl.Http;
using Kantan.Net.Utilities;

// ReSharper disable CognitiveComplexity

// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo

namespace SmartImage.Lib.Engines.Search;

public sealed class Ascii2DEngine : BaseSearchEngine, IWebContentEngine<INode>
{
	public Ascii2DEngine() : base("https://ascii2d.net/search/url/")
	{
		Timeout = TimeSpan.FromSeconds(6);
		MaxSize = 5 * 1000 * 1000;
	}

	public string NodesSelector => "//*[contains(@class, 'info-box')]";

	public override SearchEngineOptions EngineOption => SearchEngineOptions.Ascii2D;

	protected override bool VerifyImage(Image i)
	{
		return i.PhysicalDimension is { Width: < 10000.0f };
	}

	protected override async Task<Url> GetRawUrlAsync(SearchQuery query)
	{
		var url = await base.GetRawUrlAsync(query);

		url = url.SetQueryParams(new
		{
			type = "color"
		});

		/*
		 * URL parameters
		 *
		 * color	https://ascii2d.net/search/color/<hash>
		 * detail	https://ascii2d.net/search/bovw/<hash>
		 *
		 */

		return url;
	}

	public override void Dispose() { }

	public Task<SearchResultItem> ParseResultItemAsync(INode n, SearchResult r)
	{
		var sri = new SearchResultItem(r);

		var info = n.ChildNodes.Where(n => !String.IsNullOrWhiteSpace(n.TextContent))
		            .ToArray();

		string hash = info.First().TextContent;

		// ir.OtherMetadata.Add("Hash", hash);

		string[] data = info[1].TextContent.Split(' ');

		string[] res = data[0].Split('x');
		sri.Width  = Int32.Parse(res[0]);
		sri.Height = Int32.Parse(res[1]);

		string fmt = data[1];

		string size = data[2];

		if (info.Length >= 3) {
			var node2 = info[2];
			var desc  = info.Last().FirstChild;
			var ns    = desc.NextSibling;

			if (node2.ChildNodes.Length >= 2 && node2.ChildNodes[1].ChildNodes.Length >= 2) {
				var node2Sub = node2.ChildNodes[1];

				if (node2Sub.ChildNodes.Length >= 8) {
					sri.Description = node2Sub.ChildNodes[3].TextContent.Trim();
					sri.Artist      = node2Sub.ChildNodes[5].TextContent.Trim();
					sri.Site        = node2Sub.ChildNodes[7].TextContent.Trim();
				}
			}

			if (ns.ChildNodes.Length >= 4) {
				var childNode = ns.ChildNodes[3];

				string l1 = ((IHtmlElement) childNode).GetAttribute(Resources.Atr_href);

				if (l1 is not null) {
					sri.Url = new Url(l1);
				}
			}
		}

		return Task.FromResult(sri);
	}
}