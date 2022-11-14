using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Content;
using Kantan.Net.Utilities;
using Kantan.Threading;

namespace SmartImage.Lib.Engines.Search;

public sealed class EHentaiEngine : WebContentSearchEngine
{
	public EHentaiEngine() : base("https://exhentai.org/") { }

	public override SearchEngineOptions EngineOption => default;

	public override          void      Dispose() { }

	/*
	 * https://gitlab.com/NekoInverter/EhViewer/-/tree/master/app/src/main/java/com/hippo/ehviewer/client
	 * https://gitlab.com/NekoInverter/EhViewer/-/tree/master/app/src/main/java/com/hippo/ehviewer
	 * https://gitlab.com/NekoInverter/EhViewer/-/blob/master/app/src/main/java/com/hippo/ehviewer/client/EhUrl.java
	 * https://gitlab.com/NekoInverter/EhViewer/-/blob/master/app/src/main/java/com/hippo/ehviewer/client/EhEngine.java
	 * https://gitlab.com/NekoInverter/EhViewer/-/blob/master/app/src/main/java/com/hippo/ehviewer/EhApplication.java
	 * https://gitlab.com/NekoInverter/EhViewer/-/blob/master/app/src/main/java/com/hippo/ehviewer/client/data/ListUrlBuilder.java
	 * https://gitlab.com/NekoInverter/EhViewer/-/blob/master/app/src/main/java/com/hippo/ehviewer/client/EhCookieStore.java
	 */

	public async Task<SearchResult> SearchImage(Stream s, Dictionary<string, string> cj)
	{
		var sr = new SearchResult(this);

		var data = new MultipartFormDataContent()
			{ };
		// data.Add(new FileContent(f.FullName), "sfile", "a.jpg");
		data.Add(new StreamContent(s), "sfile", "a.jpg");
		data.Add(new StringContent("fs_similar"));
		data.Add(new StringContent("fs_similar"));
		data.Add(new StringContent("fs_similar"));
		data.Add(new StringContent("fs_sfile"));

		const string uri = "https://exhentai.org/upld/image_lookup.php";

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
			settings.Redirects.MaxAutoRedirects           = 10;   // default 10 (consecutive)
		});

		q = cj.Aggregate(q, (current, kv) => current.WithCookie(kv.Key, kv.Value));
		// TODO: Flurl throws an exception because it detects "circular redirects"
		// https://github.com/tmenier/Flurl/issues/714

		var res   = await q.SendAsync(HttpMethod.Post, data);*/

		/*var flurl = FlurlHttp.GlobalSettings.HttpClientFactory;
		var mh   = flurl.CreateMessageHandler();
		var cl    = flurl.CreateHttpClient(mh);
		*/
		using var clientHandler = new HttpClientHandler
		{
			AllowAutoRedirect              = true,
			MaxAutomaticRedirections       = 15,
			CheckCertificateRevocationList = false,
			UseCookies                     = true,
			CookieContainer                = new() { },
		};

		foreach (var c in cj.Select(c => new Cookie(c.Key, c.Value, domain: ((new Uri(uri).Host)), path: "/"))) {
			clientHandler.CookieContainer.Add(c);
		}

		var cl = new HttpClient(clientHandler) { };

		var req = new HttpRequestMessage(HttpMethod.Post, uri)
		{
			Content = data,
			Headers =
			{
				{ "User-Agent", HttpUtilities.UserAgent }

			}
		};

		var res = await cl.SendAsync(req);

		var content = await res.Content.ReadAsStringAsync();

		return sr;
	}

	#region Overrides of WebContentSearchEngine

	protected override string NodesSelector => ".gl1t";

	protected override async Task<SearchResultItem> ParseResultItemAsync(INode n, SearchResult r)
	{

		return default;
	}

	#endregion
}