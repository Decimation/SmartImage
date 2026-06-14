// Author: Deci | Project: SmartImage.Lib | Name: Ascii2DEngine.cs
// Date: 2024/06/06 @ 14:06:00

using System.Net;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using FlareSolverrSharp.Types;
using Flurl.Http;
using Kantan.Net.Utilities;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Clients;
using SmartImage.Lib.Cookies;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Engines.Search.Base;

// ReSharper disable CognitiveComplexity
// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo

namespace SmartImage.Lib.Engines.Search;

// todo

public sealed class Ascii2DEngine : WebSearchEngine<Ascii2DItem, IList<INode>>, ICookiesReceiver, ISearchConfigReceiver
{

	public override SearchEngineOptions Option => SearchEngineOptions.Ascii2D;

	public CookieJar Jar { get; }

	public const string ALT_URL = "https://ascii2d.obfs.dev/search/url/";

	public const string MAIN_URL = "https://ascii2d.net/search/url/";

	private readonly FlareSolverrClient m_fsClient;

	public Ascii2DEngine(ICookiesSource cookiesSource = null) : base(MAIN_URL)
	{
		Timeout       = TimeSpan.FromSeconds(30);
		MaxLength     = 10_000_000;
		Jar           = new CookieJar();
		m_fsClient    = new FlareSolverrClient();
		CookiesSource = cookiesSource ?? new ListCookiesSource();

	}

	public ICookiesSource CookiesSource { get; set; }

	// public const int MAX_WIDTH = 1000;

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


	protected override ValueTask<IList<INode>> ParseDataAsync(IDocument src)
	{
		var nodes = src.Body.SelectNodes(Serialization.S_Ascii2D_Images2);

		var cnt = nodes.RemoveAll(static x =>
		{
			var e = x as IHtmlElement;

			var b = e.Children is { Length: 1 } && e.Children[0].ClassName.Contains("hidden-md");

			return b;
		});

		return ValueTask.FromResult<IList<INode>>(nodes);
	}

	protected override ValueTask<IEnumerable<Ascii2DItem>> ParseItemsAsync(IList<INode> source, SearchResult r)
	{
		return ValueTask.FromResult(source.Select(node => Ascii2DItem.ParseSource(node, r)));
	}

	protected override async Task<IDocument> GetSourceAsync(SearchResult sr, SearchQuery query, CancellationToken token = default)
	{
		var parser = new HtmlParser();

		try {

			var origin = sr.RawUrl;

			string str = null;

			if (m_fsClient.IsInitialized) {

				try {
					var msg = new HttpRequestMessage(HttpMethod.Get, origin);

					var fsr     = await m_fsClient.Clearance.Solverr.SolveAsync(msg).ConfigureAwait(false);
					var cookies = fsr.Solution.Cookies;
					var newUrl  = fsr.Solution.Url;

					Logger.LogTrace("{Name} using {Fs}: {CookieCnt} {NewUrl}", Name, fsr, cookies.Length, newUrl);

					foreach (FlareSolverrCookie cookie in cookies) {
						Jar.AddOrReplace(new FlurlCookie(cookie.Name, cookie.Value, fsr.Solution.Url));
					}
				}
				catch (Exception e) {
					Logger.LogError(e, "{Name}", Name);
				}
			}

			using var res = await GetResponseByUrlAsync(origin, token).ConfigureAwait(false);

			if (res.StatusCode == (int) HttpStatusCode.BadGateway) {
				return null;
			}

			str = await res.GetStringAsync().ConfigureAwait(false);

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
			Logger.LogError(e, "{Name} error in {Fn}", Name, nameof(GetSourceAsync));
			return null;
		}
	}

	private async Task<IFlurlResponse> GetResponseByUrlAsync(Url origin, CancellationToken token)
	{
		var res = await Client.Request(origin)
		                      .AllowAnyHttpStatus()
		                      .WithCookies(Jar)
		                      .WithTimeout(Timeout)
		                      .GetAsync(cancellationToken: token)
		                      .ConfigureAwait(false);
		return res;
	}

	public override async ValueTask<bool> ApplyConfigAsync(SearchConfig cfg, CancellationToken ct = default)
	{
		var b = await base.ApplyConfigAsync(cfg, ct);

		b |= await m_fsClient.ApplyConfigAsync(cfg, ct);

		return b;
	}

	public override void Dispose()
	{
		m_fsClient?.Dispose();
	}

}

public record Ascii2DItem : SearchResultItem, IParseableResult<INode, Ascii2DItem>
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

		sri.Thumbnail = Url.Combine(r.Engine.Url.Root, thumb?.Value);

		var info = n.ChildNodes.Where(static n1 => !String.IsNullOrWhiteSpace(n1.TextContent))
		            .ToArray();

		sri.HashString = info.First().TextContent;

		// ir.OtherMetadata.Add("Hash", hash);

		string[] data = info[1].TextContent.Split(' ');

		string[] res = data[0].Split('x');
		sri.Width  = Int32.Parse(res[0]);
		sri.Height = Int32.Parse(res[1]);

		sri.Format = data[1];

		string size   = data[2];
		string title1 = (n as IHtmlElement)?.FirstChild.TryGetAttribute("Title");

		if (info.Length >= 3) {
			var node2 = info[2];
			var desc  = info.Last().FirstChild;
			var ns    = desc.NextSibling;

			if (node2.ChildNodes is [_, { ChildNodes.Length: >= 2 }, ..]) {
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