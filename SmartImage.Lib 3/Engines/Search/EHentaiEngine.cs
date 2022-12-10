using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Content;
using Kantan.Net.Utilities;
using Kantan.Text;
using Kantan.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SmartImage.Lib.Engines.Search;

public sealed class EHentaiEngine : BaseSearchEngine, IWebContentEngine, ILoginEngine
{
	private const string EHENTAI_INDEX_URI         = "https://forums.e-hentai.org/index.php";
	private const string EXHENTAI_IMAGE_LOOKUP_URI = "https://exhentai.org/upld/image_lookup.php";
	private const string EXHENTAI_URI              = "https://exhentai.org/";
	private const string EHENTAI_URI               = "https://e-hentai.org/";

	public EHentaiEngine() : base(EXHENTAI_URI)
	{
		Cookies      = new CookieJar();
		m_isLoggedIn = false;
	}

	public CookieJar Cookies { get; }

	public override SearchEngineOptions EngineOption => SearchEngineOptions.EHentai;

	public string Username { get; set; }
	public string Password { get; set; }

	private bool m_isLoggedIn;

	public bool IsLoggedIn => m_isLoggedIn;

	public string NodesSelector => "//table/tbody/tr";

	/*
	 * https://gitlab.com/NekoInverter/EhViewer/-/tree/master/app/src/main/java/com/hippo/ehviewer/client
	 * https://gitlab.com/NekoInverter/EhViewer/-/tree/master/app/src/main/java/com/hippo/ehviewer
	 * https://gitlab.com/NekoInverter/EhViewer/-/blob/master/app/src/main/java/com/hippo/ehviewer/client/EhUrl.java
	 * https://gitlab.com/NekoInverter/EhViewer/-/blob/master/app/src/main/java/com/hippo/ehviewer/client/EhEngine.java
	 * https://gitlab.com/NekoInverter/EhViewer/-/blob/master/app/src/main/java/com/hippo/ehviewer/EhApplication.java
	 * https://gitlab.com/NekoInverter/EhViewer/-/blob/master/app/src/main/java/com/hippo/ehviewer/client/data/ListUrlBuilder.java
	 * https://gitlab.com/NekoInverter/EhViewer/-/blob/master/app/src/main/java/com/hippo/ehviewer/client/EhCookieStore.java
	 */

	public async Task<IDocument> GetDocumentAsync(object origin, SearchQuery query,
	                                              TimeSpan? timeout = null, CancellationToken? token = null)
	{
		var data = new MultipartFormDataContent()
		{
			{ new FileContent(query.Uni.Value.ToString()), "sfile", "a.jpg" },
			// { new StreamContent((Stream) query.Uni.Stream), "sfile", "a.jpg" },
			{ new StringContent("fs_similar") },
			{ new StringContent("fs_covers") },
			{ new StringContent("fs_exp") },
			{ new StringContent("fs_sfile") }
		};

		// data.Add(new FileContent(f.FullName), "sfile", "a.jpg");

		/*var q = uri
		        .WithHeaders(new
		        {
			        User_Agent = HttpUtilities.UserAgent
		        }).WithAutoRedirect(true);

		FlurlHttp.Configure(settings =>
		{
			settings.Redirects.Enabled                    = true; // default true
			settings.Redirects.AllowSecureToInsecure      = true; // default false
			settings.Redirects.ForwardAuthorizationHeader = true; // default false
			settings.Redirects.MaxAutoRedirects           = 20;   // default 10 (consecutive)
		});

		q = cj.Aggregate(q, (current, kv) => current.WithCookie(kv.Key, kv.Value));
		
		// TODO: Flurl throws an exception because it detects "circular redirects"
		// https://github.com/tmenier/Flurl/issues/714

		var res   = await q.SendAsync(HttpMethod.Post, data);*/

		// var flurl = FlurlHttp.GlobalSettings.HttpClientFactory;
		// var mh   = flurl.CreateMessageHandler();
		// var cl    = flurl.CreateHttpClient(mh);

		using var clientHandler = new HttpClientHandler
		{
			AllowAutoRedirect              = true,
			MaxAutomaticRedirections       = 15,
			CheckCertificateRevocationList = false,
			UseCookies                     = true,
			CookieContainer                = new() { },
		};

		foreach (var c in Cookies) {
			clientHandler.CookieContainer.Add(new Cookie(c.Name, c.Value, c.Path, c.Domain));
		}

		var cl = new HttpClient(clientHandler) { };

		var req = new HttpRequestMessage(HttpMethod.Post, EXHENTAI_IMAGE_LOOKUP_URI)
		{
			Content = data,
			Headers =
			{
				{ "User-Agent", HttpUtilities.UserAgent }
			},
		};

		var res = await cl.SendAsync(req);

		var content = await res.Content.ReadAsStringAsync();

		if (content.Contains("Please wait a bit longer between each file search.")) {
			Debug.WriteLine($"cooldown", Name);
			return null;
		}

		var parser = new HtmlParser();
		return await parser.ParseDocumentAsync(content);
	}

	private async Task<IFlurlResponse> GetSessionAsync()
	{
		return await EXHENTAI_URI.WithCookies(Cookies)
		                           .WithHeaders(new
		                           {
			                           User_Agent = HttpUtilities.UserAgent
		                           })
		                           .WithAutoRedirect(true)
		                           .GetAsync();
	}

	/*
	 * Default result layout is [Compact]
	 */

	public async Task LoginAsync()
	{
		var content = new MultipartFormDataContent()
		{
			{ new StringContent("1"), "CookieDate" },
			{ new StringContent("d"), "b" },
			{ new StringContent("1-6"), "bt" },
			{ new StringContent(Username), "UserName" },
			{ new StringContent(Password), "PassWord" },
			{ new StringContent("Login!"), "ipb_login_submit" }
		};

		var response = await EHENTAI_INDEX_URI
		                     .SetQueryParams(new
		                     {
			                     act  = "Login",
			                     CODE = 01
		                     }).WithHeaders(new
		                     {
			                     User_Agent = HttpUtilities.UserAgent
		                     })
		                     .WithCookies(out var cj)
		                     .PostAsync(content);

		foreach (FlurlCookie fc in cj) {
			Cookies.AddOrReplace(fc);
		}

		var res2 = await GetSessionAsync();

		m_isLoggedIn = true;
	}

	public Task<List<INode>> GetNodes(IDocument d)
	{
		// Index 0 is table header
		var nodes = d.Body.SelectNodes(NodesSelector).Skip(1).ToList();

		return Task.FromResult(nodes);
	}

	public async Task<SearchResultItem> ParseNodeToItem(INode n, SearchResult r)
	{
		var item = new SearchResultItem(r)
			{ };

		EhResult eh = GetEhResult(n);

		if (eh.Tags.TryGetValue("artist", out var v)) {
			item.Artist = v.FirstOrDefault();
		}

		item.Description = new[] { eh.Pages, $"({eh.Type})" }.QuickJoin(" ");
		item.Title       = eh.Title;
		item.Url         = eh.Url;

		/*var gl1c        = n.ChildNodes[0];
		var gl2c        = n.ChildNodes[1];
		var ehx_compact = n.ChildNodes[2];
		var gl3c        = n.ChildNodes[3];
		var gl4c        = n.ChildNodes[4];*/

		return item;
	}

	private static EhResult GetEhResult(INode n)
	{
		var eh = new EhResult();

		var gl1c = n.ChildNodes.FirstOrDefault(f => f is IElement { ClassName: "gl1c" } e);

		if (gl1c is { }) {
			if (gl1c.FirstChild is { } t) {
				eh.Type = t.TextContent;
			}
		}

		var gl2c = n.ChildNodes.FirstOrDefault(f => f is IElement { ClassName: "gl2c" } e);

		if (gl2c is { }) {
			if (gl2c.ChildNodes[1].ChildNodes[1].ChildNodes[1].ChildNodes[1] is { } div) {
				eh.Pages = div.TextContent;
			}
		}

		var gl3c = n.ChildNodes.FirstOrDefault(f => f is IElement { ClassName: "gl3c glname" } e);

		if (gl3c is { }) {
			if (gl3c.FirstChild is { } f) {
				eh.Url = (Url) f.TryGetAttribute(Resources.Atr_href);

				if (f.FirstChild is { } ff) {
					eh.Title = ff.TextContent;
				}

				if (f.ChildNodes[1] is { ChildNodes: { Length: > 0 } cn } f2) {
					var tagValuesRaw = cn.Select(c => c.TryGetAttribute("title"));

					foreach (string s in tagValuesRaw) {
						var split = s.Split(':');
						var tag   = split[0];
						var val   = split[1];

						if (eh.Tags.ContainsKey(tag)) {
							eh.Tags[tag].Add(val);
						}
						else {
							eh.Tags.Add(tag, new List<string>() { val });

						}
					}
				}
			}
		}

		var gl4c = n.ChildNodes.FirstOrDefault(f => f is IElement { ClassName: "gl4c glhide" } e);

		if (gl4c is { }) {
			if (gl4c.ChildNodes[0] is { FirstChild: { } div1 } div1Outer) {
				eh.AuthorUrl = div1.TryGetAttribute(Resources.Atr_href);
				eh.Author    = div1Outer.TextContent ?? div1.TextContent;
			}

			if (gl4c.ChildNodes[1] is { } div2) {
				eh.Pages ??= div2.TextContent;
			}
		}

		return eh;
	}

	public override void Dispose() { }

	private sealed record EhResult
	{
		internal string Type      { get; set; }
		internal string Pages     { get; set; }
		internal string Title     { get; set; }
		internal string Author    { get; set; }
		internal string AuthorUrl { get; set; }
		internal Url    Url       { get; set; }

		internal Dictionary<string, List<string>> Tags { get; set; } = new();
	}
}