#nullable disable
using System.Diagnostics;
using System.Drawing;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Flurl.Http;
using Kantan.Net.Utilities;
using SmartImage.Lib.Results;
using Image = System.Drawing.Image;

// ReSharper disable CognitiveComplexity

// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo

namespace SmartImage.Lib.Engines.Impl.Search;

// todo

public sealed class Ascii2DEngine : WebSearchEngine
{
	public Ascii2DEngine() : base("https://ascii2d.net/search/url/")
	{
		Timeout = TimeSpan.FromSeconds(6);
		MaxSize = 5 * 1000 * 1000;
	}

	protected override string NodesSelector => Serialization.S_Ascii2D_Images;

	public override SearchEngineOptions EngineOption => SearchEngineOptions.Ascii2D;

	protected override bool VerifyImage(Image i)
	{
#if ALT
#pragma warning disable CA1416

		return i.PhysicalDimension is { Width: < 10000.0f };
#pragma warning restore
#else
		return true;
#endif

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

	protected override async Task<IDocument> GetDocumentAsync(object sender, SearchQuery query,
	                                                          CancellationToken token = default)
	{

		var parser = new HtmlParser();

		try {
			if (sender is Url origin) {
				var data = new MultipartFormDataContent()
				{
					{ new StringContent(origin), "uri" }
				};

				var res = await origin.AllowAnyHttpStatus()
					          .WithCookies(out var cj)
					          .WithTimeout(Timeout)
					          .WithHeaders(new
					          {
						          User_Agent = HttpUtilities.UserAgent
					          })
					          .WithAutoRedirect(true)
					          .WithClient(SearchClient.Client)
					          /*.OnError(s =>
					          {
						          Debug.WriteLine($"{s.Response}");
						          s.ExceptionHandled = true;
								  
					          })*/
					          .GetAsync(token);

				var str = await res.GetStringAsync();

				var document = await parser.ParseDocumentAsync(str, token);

				return document;

			}
			else {
				return null;
			}
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

	protected override string[] ErrorBodyMessages => new[] { "検索できるのは 縦 10000px での画像です。" };

	protected override ValueTask<SearchResultItem> ParseResultItem(INode n, SearchResult r)
	{
		var sri = new SearchResultItem(r);

		var info = n.ChildNodes.Where(n => !string.IsNullOrWhiteSpace(n.TextContent))
			.ToArray();

		string hash = info.First().TextContent;

		// ir.OtherMetadata.Add("Hash", hash);

		string[] data = info[1].TextContent.Split(' ');

		string[] res = data[0].Split('x');
		sri.Width  = int.Parse(res[0]);
		sri.Height = int.Parse(res[1]);

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

				string l1 = ((IHtmlElement) childNode).GetAttribute(Serialization.Atr_href);

				if (l1 is not null) {
					sri.Url = new Url(l1);
				}
			}
		}

		return ValueTask.FromResult(sri);
	}
}