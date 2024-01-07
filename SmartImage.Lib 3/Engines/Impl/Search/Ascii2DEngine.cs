#nullable disable
using System.Diagnostics;
using System.Drawing;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Flurl.Http;
using Flurl.Http.Content;
using Kantan.Net.Utilities;
using SmartImage.Lib.Results;

// ReSharper disable CognitiveComplexity

// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo

namespace SmartImage.Lib.Engines.Impl.Search;

// todo

public sealed class Ascii2DEngine : WebSearchEngine
{

	public Ascii2DEngine() : base("https://ascii2d.net/search/url/")
	{
		Timeout = TimeSpan.FromSeconds(10);
		MaxSize = 5 * 1000 * 1000;
	}

	protected override string NodesSelector => Serialization.S_Ascii2D_Images2;

	public override SearchEngineOptions EngineOption => SearchEngineOptions.Ascii2D;

	public const int MAX_WIDTH = 1000;

	protected override bool VerifyQuery(SearchQuery q)
	{
		var  b = base.VerifyQuery(q);
		bool b2;
		bool ok = q.HasImage;

		if (!ok) {
			ok = q.LoadImage();

		}
		if (ok) {
			b2 = q.ImageInfo.Width < MAX_WIDTH;
		}
		else {
			b2 = true;
		}

		return b && b2;
	}

	protected override async ValueTask<Url> GetRawUrlAsync(SearchQuery query)
	{
		var url = await base.GetRawUrlAsync(query);

		/*url = url.SetQueryParams(new
		{
			type = "color"
		});*/

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

	protected override async Task<IDocument> GetDocumentAsync(SearchResult sr, SearchQuery query,
	                                                          CancellationToken token = default)
	{

		var parser = new HtmlParser();

		try {
			IFlurlResponse res;

			res = await GetResponseByUrlAsync(sr.RawUrl, token);

			var str = await res.GetStringAsync();

			var document = await parser.ParseDocumentAsync(str, token);

			return document;
		}
		catch (TaskCanceledException) {
			return null;

		}
		catch (FlurlHttpException e) {
			// return await Task.FromException<IDocument>(e);
			Debug.WriteLine($"{this} :: {e.Message}", nameof(GetDocumentAsync));

			return null;
		}
	}

	private async Task<IFlurlResponse> GetResponseByUrlAsync(Url origin, CancellationToken token)
	{
		var data = new MultipartFormDataContent()
		{
			{ new StringContent(origin), "uri" }
		};

		var res = await Client.Request(origin).AllowAnyHttpStatus()
			          .WithCookies(out var cj)
			          .WithTimeout(Timeout)
			          .WithHeaders(new
			          {
				          User_Agent = HttpUtilities.UserAgent
			          })
			          .WithAutoRedirect(true)
			          /*.OnError(s =>
					          {
						          Debug.WriteLine($"{s.Response}");
						          s.ExceptionHandled = true;

					          })*/
			          .GetAsync(cancellationToken: token);
		return res;
	}

	protected override string[] ErrorBodyMessages
		=>
		[
			"検索できるのは 縦 10000px での画像です。",
			"ごく最近、このURLからのダウンロードに失敗しています。少し時間を置いてください。"
		];

	protected override ValueTask<SearchResultItem> ParseResultItem(INode nx, SearchResult r)
	{
		var sri = new SearchResultItem(r);
		var nxe = nx as IHtmlElement;

		var n      = nxe.Children[1];
		var imgBox = nxe.Children[0];
		var thumb  = imgBox.Children[0].Attributes["src"];
		sri.Thumbnail = Url.Combine(BaseUrl.Root, thumb?.Value);

		var info = n.ChildNodes.Where(n1 => !string.IsNullOrWhiteSpace(n1.TextContent))
			.ToArray();

		string hash = info.First().TextContent;

		// ir.OtherMetadata.Add("Hash", hash);

		string[] data = info[1].TextContent.Split(' ');

		string[] res = data[0].Split('x');
		sri.Width  = int.Parse(res[0]);
		sri.Height = int.Parse(res[1]);

		string fmt = data[1];

		string size   = data[2];
		string title1 = (n as IHtmlElement).FirstChild.TryGetAttribute("Title");

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

				string l1 = ((IHtmlElement) childNode).GetAttribute(Serialization.Atr_href);

				if (l1 is not null) {
					sri.Url  =   new Url(l1);
					sri.Site ??= sri.Url.Host;
				}
			}
		}

		return ValueTask.FromResult(sri);
	}

}