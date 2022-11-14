using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Content;
using Kantan.Net.Utilities;
using Kantan.Threading;

namespace SmartImage.Lib.Engines.Search;

public sealed class EHentaiEngine : BaseSearchEngine
{
	public EHentaiEngine() : base("https://exhentai.org/") { }

	public override SearchEngineOptions EngineOption => default;

	public override void Dispose() { }

	public async Task<SearchResult> SearchImage(Stream u, Dictionary<string, string> cj)
	{
		var sr = new SearchResult(this);

		var data = new MultipartFormDataContent()
			{ };
		data.Add(new StreamContent(u));

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
			Content = data
		};

		req.Headers.Add("User-Agent", HttpUtilities.UserAgent);

		var res= await cl.SendAsync(req);

		var content = await res.Content.ReadAsStringAsync();

		return sr;
	}
}