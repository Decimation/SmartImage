using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flurl.Http;
using Kantan.Net.Utilities;

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

		var q = "https://exhentai.org/upld/image_lookup.php"
		        .WithHeaders(new
		        {
			        User_Agent = HttpUtilities.UserAgent
		        }).WithAutoRedirect(true);
		FlurlHttp.Configure(settings => {
			settings.Redirects.Enabled                    = true; // default true
			settings.Redirects.AllowSecureToInsecure      = true; // default false
			settings.Redirects.ForwardAuthorizationHeader = true; // default false
			settings.Redirects.MaxAutoRedirects           = 10;   // default 10 (consecutive)
		});
		q = cj.Aggregate(q, (current, kv) => current.WithCookie(kv.Key, kv.Value));
		// TODO: Flurl throws an exception because it detects "circular redirects"
		// https://github.com/tmenier/Flurl/issues/714

		var res = await q.SendAsync(HttpMethod.Post,data);

		return sr;
	}
}