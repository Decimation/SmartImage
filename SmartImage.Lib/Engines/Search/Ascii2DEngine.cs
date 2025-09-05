// Author: Deci | Project: SmartImage.Lib | Name: Ascii2DEngine.cs
// Date: 2024/06/06 @ 14:06:00

using System.Diagnostics;
using System.Net;
using System.Xml.Linq;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using FlareSolverrSharp.Types;
using Flurl.Http;
using Kantan.Net.Utilities;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.Formats.Bmp;
using SmartImage.Lib.Clients;
using SmartImage.Lib.Cookies;
using SmartImage.Lib.Engines.Results;

// ReSharper disable CognitiveComplexity

// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo

namespace SmartImage.Lib.Engines.Search;

// todo

public sealed class Ascii2DEngine : WebSearchEngine<Ascii2DItem, IList<INode>>, ICookiesReceiver
{

	public override SearchEngineOptions EngineOption => SearchEngineOptions.Ascii2D;

	public CookieJar Jar { get; }

	protected override string[] ErrorBodyMessages
		=>
		[
			"検索できるのは 縦 10000px での画像です。",
			"ごく最近、このURLからのダウンロードに失敗しています。少し時間を置いてください。"
		];

	public const string ALT_URL = "https://ascii2d.obfs.dev/search/url/";

	public const string MAIN_URL = "https://ascii2d.net/search/url/";

	public Ascii2DEngine() : base(MAIN_URL)
	{
		Timeout = TimeSpan.FromSeconds(30);
		MaxSize = 10_000_000;
		Jar     = new CookieJar();
	}

	public async ValueTask<bool> ApplyCookiesAsync(ICookiesSource source, CancellationToken ct)
	{
		if ( /*FlareSolverrClient.Value.IsInitialized*/ source == null) {
			return false;
		}

		var cookies = await source.GetOrLoadCookiesAsync(ct).ConfigureAwait(false);

		foreach (var bck in cookies) {
			var ck = bck.AsCookie();

			if (ck.Domain.Contains("ascii2d")) {
				Jar.AddOrReplace(new FlurlCookie(ck.Name, ck.Value, BaseUrl));
			}
		}


		return true;
	}

	public override ValueTask<bool> ApplyConfigAsync(SearchConfig cfg, CancellationToken ct = default)
	{
		return ValueTask.FromResult(true);
	}

	public override void Dispose() { }

	// public const int MAX_WIDTH = 1000;

	/*protected override bool VerifyQuery(SearchQuery q)
	{
		var  b = base.VerifyQuery(q);
		bool b2;
		bool ok = q.HasImage;

		if (!ok) {
			ok = q.AllocImage();

		}
		if (ok) {
			// b2 = q.ImageInfo.Width < MAX_WIDTH;
		}
		else {
			b2 = true;
		}

		return b && b2;
	}*/

	protected override Url GetRawUrl(SearchQuery query)
	{
		var url = base.GetRawUrl(query);

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


	protected override ValueTask<IList<INode>> ParseIntermediate(IDocument d)
	{
		var nodes = d.Body.SelectNodes(Serialization.S_Ascii2D_Images2);

		var cnt = nodes.RemoveAll(static x =>
		{
			var e = x as IHtmlElement;

			var b = e.Children is { Length: 1 } && e.Children[0].ClassName.Contains("hidden-md");

			return b;
		});

		return ValueTask.FromResult<IList<INode>>(nodes);
	}

	protected override ValueTask<IEnumerable<Ascii2DItem>> ParseResultItems(IList<INode> source, SearchResult r)
	{
		var buf = new List<Ascii2DItem>(source.Count);

		foreach (var node in source) {
			var item = Ascii2DItem.ParseSource(node, r);
			buf.Add(item);
		}

		return ValueTask.FromResult<IEnumerable<Ascii2DItem>>(buf);
	}

	protected override async Task<IDocument> GetSourceAsync(SearchResult sr, SearchQuery query,
	                                                        CancellationToken token = default)
	{
		var parser = new HtmlParser();

		try {

			var origin = sr.RawUrl;

			string str = null;

			/*var res = await new HttpClient(new FlareSolverrHandler()).SendAsync(
				          new HttpRequestMessage(HttpMethod.Get, origin));

			if (res != null) {
				var fsr = await res.GetJsonAsync<FlareSolverrRoot>();
				str = fsr.Solution.Response;
			}
			else {
				res = await GetResponseByUrlAsync(origin, token);
				str = await res.GetStringAsync();

			}*/

			if (FlareSolverrClient.Value.IsInitialized) {

				var msg = new HttpRequestMessage(HttpMethod.Get, origin);

				var fsr     = await FlareSolverrClient.Value.Clearance.Solverr.SolveAsync(msg).ConfigureAwait(false);
				var cookies = fsr.Solution.Cookies;
				var newUrl  = fsr.Solution.Url;


				foreach (FlareSolverrCookie cookie in cookies) {
					Jar.AddOrReplace(new FlurlCookie(cookie.Name, cookie.Value, fsr.Solution.Url));
				}

				using var res = await Client.Request(newUrl)
					                .WithSettings(static x => { x.HttpVersion = "2.0"; })
					                .AllowAnyHttpStatus()
					                .WithCookies(Jar)
					                .WithTimeout(Timeout)
					                /*.OnError(s =>
							                {
								                Debug.WriteLine($"{s.Response}");
								                s.ExceptionHandled = true;

							                })*/
					                .GetAsync(cancellationToken: token).ConfigureAwait(false);


				// var res1 = await FlareSolverrClient.Client.SendAsync(msg, token);
				// str = await res1.Content.ReadAsStringAsync(token);
				str = await res.GetStringAsync().ConfigureAwait(false);

			}
			else {
				using var res = await GetResponseByUrlAsync(origin, token).ConfigureAwait(false);

				if (res.StatusCode == (int) HttpStatusCode.BadGateway) {
					return null;
				}

				str = await res.GetStringAsync().ConfigureAwait(false);
			}

			var document = await parser.ParseDocumentAsync(str, token).ConfigureAwait(false);

			return document;
		}
		catch (ArgumentException) {
			return null;
		}
		catch (TaskCanceledException) {
			return null;

		}
		catch (FlurlHttpException e) {
			// return await Task.FromException<IDocument>(e);
			// Debug.WriteLine($"{this} :: {e.Message}", nameof(GetDocumentAsync));
			Logger.LogError(e, "{Name} error in {Fn}", Name, nameof(GetSourceAsync));
			return null;
		}
	}

	private async Task<IFlurlResponse> GetResponseByUrlAsync(Url origin, CancellationToken token)
	{
		var res = await Client.Request(origin)
			          .AllowAnyHttpStatus()
			          .WithCookies(out var cj)
			          .WithTimeout(Timeout)
			          .GetAsync(cancellationToken: token).ConfigureAwait(false);
		return res;
	}

}

public class Ascii2DItem : SearchResultItem, IResultItemParseable<INode, Ascii2DItem>
{

	public string HashString { get; private set; }

	public string Format { get; private set; }

	private Ascii2DItem(SearchResult r) : base(r) { }

	public static Ascii2DItem ParseSource(INode nx, SearchResult r)
	{
		var sri = new Ascii2DItem(r);

		var nxe = nx as IHtmlElement;

		var n      = nxe.Children[1];
		var imgBox = nxe.Children[0];
		var thumb  = imgBox.Children[0].Attributes["src"];

		sri.Thumbnail = Url.Combine(r.Engine.BaseUrl.Root, thumb?.Value);

		var info = n.ChildNodes.Where(static n1 => !string.IsNullOrWhiteSpace(n1.TextContent))
			.ToArray();

		sri.HashString = info.First().TextContent;

		// ir.OtherMetadata.Add("Hash", hash);

		string[] data = info[1].TextContent.Split(' ');

		string[] res = data[0].Split('x');
		sri.Width  = int.Parse(res[0]);
		sri.Height = int.Parse(res[1]);

		sri.Format = data[1];

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

		return sri;
	}

}