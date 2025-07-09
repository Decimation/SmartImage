// Author: Deci | Project: SmartImage.Lib | Name: EHentaiEngine.cs
// Date: 2024/06/06 @ 14:06:00

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Flurl.Http;
using Flurl.Http.Content;
using Kantan.Net.Utilities;
using Kantan.Text;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Cookies;
using SmartImage.Lib.Results;
using SmartImage.Lib.Results.Data;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Engines.Impl.Search;

/// <summary>
///     <see cref="SearchEngineOptions.EHentai" />
/// </summary>
/// <remarks>Handles both ExHentai and E-Hentai</remarks>
public sealed class EHentaiEngine : WebSearchEngine, INotifyPropertyChanged, ICookiesReceiver
{

	static EHentaiEngine() { }

	public EHentaiEngine(bool useExHentai = true) : base(EHentaiBase)
	{
		IsLoggedIn = false;

		UseExHentai = useExHentai;
		Jar         = new CookieJar();
	}

	// NOTE: a separate HttpClient is used for EHentai because of special network requests and other unique requirements...

	public override Url BaseUrl => IsLoggedIn ? ExHentaiBase : EHentaiBase;

	private Url LookupUrl => IsLoggedIn ? ExHentaiLookup : EHentaiLookup;

	private Url OriginUrl => UseExHentai ? ExHentaiBase : EHentaiBase;

	public override SearchEngineOptions EngineOption => SearchEngineOptions.EHentai;

	protected override string NodesSelector => Serialization.S_EHentai;

	public bool IsLoggedIn { get; private set; }

	public bool UseExHentai { get; set; }

	public CookieJar Jar { get; }

	private Task<IFlurlResponse> GetSessionAsync()
	{
		return Client.Request(UseExHentai ? ExHentaiBase : EHentaiBase)
			.WithCookies(Jar)
			.WithTimeout(Timeout)
			.WithHeaders(new
			{
				User_Agent = HttpUtilities.UserAgent
			})
			.WithAutoRedirect(true)
			.GetAsync();
	}

	protected override async Task<IDocument> GetDocumentAsync(SearchResult sr, SearchQuery query,
	                                                          CancellationToken token = default)
	{

		const string SFILE_NAME_DEFAULT = "a.jpg";
		string       fileName;
		string       filePath = null;

		if (query.Source.HasFile) {
			filePath = query.Source.FilePath;
			fileName = Path.GetFileName(filePath);

			/*if (Path.GetFileName(t) != name) {
				// Debugger.Break();
			}*/
		}
		else {
			fileName = SFILE_NAME_DEFAULT;
			var ok = query.Source.TryWriteToFile(fileName);

			if (ok) {
				filePath = query.Source.FilePath;
			}
			else {
				Debugger.Break();
			}
		}

		if (filePath != null) {
			// Trace.WriteLine($"allocated {filePath}", nameof(GetDocumentAsync));
			Logger.LogTrace("Allocated {Path}", filePath);
		}

		var data = new MultipartFormDataContent
		{
			{ new FileContent(filePath), "sfile", fileName },

			// { new StreamContent((Stream) query.Uni.Stream), "sfile", "a.jpg" },
			new StringContent("fs_similar"),
			new StringContent("fs_covers"),
			new StringContent("fs_exp"),
			new StringContent("fs_sfile"),
			{ new StringContent("dm_l"), "inline_set" }
		};


		// Debug.WriteLine($"{LookupUrl}", nameof(GetDocumentAsync));

		var req = new FlurlRequest(LookupUrl)
		{
			CookieJar = Jar,
			Verb      = HttpMethod.Post,
			Content   = data,
			Headers =
			{
				{ "User-Agent", HttpUtilities.UserAgent }
			}
		};

		using var flurlRes = await Client.SendAsync(req, cancellationToken: token);
		using var httpRes  = flurlRes.ResponseMessage;

		/*using var flurlRes = await LookupUrl.
			                     WithCookies(Jar)
			                     .WithHeader("User-Agent", HttpUtilities.UserAgent)
			                     .PostMultipartAsync(bc =>
			                     {
									 //
				                     bc.AddFile(path: filePath, fileName: fileName, name: "sfile");
			                     }, cancellationToken: token);*/
		
		// using var httpRes  = flurlRes.ResponseMessage;

		// Debug.WriteLine($"{res.StatusCode}");

		sr.RawUrl = httpRes.RequestMessage.RequestUri;
		var old = sr.Results.Find(r => r.IsRaw);
		old.Url = sr.RawUrl;

		Debug.Assert(old == sr.Results[0]);

		// Debug.WriteLine($"{sr.RawUrl}");
		var content = await httpRes.Content.ReadAsStringAsync(token);

		// var content2 = await sr.RawUrl.GetStringAsync(cancellationToken: token);

		if (content.Contains("Please wait a bit longer between each file search.")) {
			// Debug.WriteLine("cooldown", Name);
			sr.Status = SearchResultStatus.Cooldown;

			return null;
		}

		var parser = new HtmlParser();
		return await parser.ParseDocumentAsync(content, token);
	}

	protected override ValueTask<IEnumerable<INode>> GetNodes(IDocument d)
	{
		// Index 0 is table header
		var array = d.Body.SelectNodes(NodesSelector);

		if (array.Count != 0) {
			array = array[1..];

		}

		return ValueTask.FromResult((IEnumerable<INode>) array);
	}

	/*
	 * Default result layout is [Compact]
	 */

#region Overrides of WebSearchEngine

	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken token = default)
	{
		return await base.GetResultAsync(query, token);
	}

	protected override Url GetRawUrl(SearchQuery query)
	{
		return base.GetRawUrl(query);
	}

#endregion

	public async ValueTask<bool> ApplyCookiesAsync(ICookiesSource source, CancellationToken ct = default)
	{
		if (source == null) {
			return false;
		}

		if (IsLoggedIn) {
			Trace.WriteLine($"Not applying cookies to {Name}; already logged in");
			return IsLoggedIn;

		}
		else {
			Trace.WriteLine($"Applying cookies to {Name}");
		}

		var cookies = await source.GetOrLoadCookiesAsync(ct);

		foreach (var bck in cookies) {

			// var cookie = bck.AsFlurlCookie(OriginUrl);

			var cookie = bck.AsCookie();

			if (cookie == null) {
				continue;
			}

			bool c = false;


			var isEx = cookie.Domain.Contains(HOST_EX);
			var isEh = cookie.Domain.Contains(HOST_EH);

			if (UseExHentai) {
				c |= isEx;
			}

			c |= isEh;

			if (c) {
				Jar.AddOrReplace(cookie.Name, cookie.Value, isEx ? ExHentaiBase : EHentaiBase);
			}
		}

		var response = await GetSessionAsync();
		return IsLoggedIn = response.ResponseMessage.IsSuccessStatusCode;

		return true;
	}

	public async Task<bool> LoginAsync(string username, string password)
	{
		/*
		if (IsLoggedIn) {
			return false;
		}
		*/

		// var fcc = await ReadCookiesAsync();

		var content = new MultipartFormDataContent()
		{
			{ new StringContent("1"), "CookieDate" },
			{ new StringContent("d"), "b" },
			{ new StringContent("1-6"), "bt" },
			{ new StringContent(username), "UserName" },
			{ new StringContent(password), "PassWord" },
			{ new StringContent("Login!"), "ipb_login_submit" }
		};

		var response = await EHentaiIndex
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

		/*foreach (var fc in fcc) {
			Cookies.Add(fc.AsCookie());
		}*/

		foreach (var fc in response.Cookies) {
			Jar.AddOrReplace(fc);
		}

		var res2 = await GetSessionAsync();

		return IsLoggedIn = res2.ResponseMessage.IsSuccessStatusCode;
	}

	/*
	 * https://gitlab.com/NekoInverter/EhViewer/-/tree/master/app/src/main/java/com/hippo/ehviewer/client
	 * https://gitlab.com/NekoInverter/EhViewer/-/tree/master/app/src/main/java/com/hippo/ehviewer
	 * https://gitlab.com/NekoInverter/EhViewer/-/blob/master/app/src/main/java/com/hippo/ehviewer/client/EhUrl.java
	 * https://gitlab.com/NekoInverter/EhViewer/-/blob/master/app/src/main/java/com/hippo/ehviewer/client/EhEngine.java
	 * https://gitlab.com/NekoInverter/EhViewer/-/blob/master/app/src/main/java/com/hippo/ehviewer/EhApplication.java
	 * https://gitlab.com/NekoInverter/EhViewer/-/blob/master/app/src/main/java/com/hippo/ehviewer/client/data/ListUrlBuilder.java
	 * https://gitlab.com/NekoInverter/EhViewer/-/blob/master/app/src/main/java/com/hippo/ehviewer/client/EhCookieStore.java
	 *
	 * https://github.com/jiangtian616/JHenTai/blob/master/lib/src/network/eh_cookie_manager.dart
	 * https://github.com/Ehviewer-Overhauled/Ehviewer/issues/873
	 */

	public override ValueTask<bool> ApplyConfigAsync(SearchConfig cfg, CancellationToken ct = default)
	{
		/*if (this is { IsLoggedIn: true }/* && !(Username != cfg.EhUsername && Password != cfg.EhPassword)#1#) {
			Debug.WriteLine($"{Name} is already logged in", nameof(ApplyConfigAsync));

			return;
		}*/
		//

		return ValueTask.FromResult(true);
	}

#region

	public static readonly Url EHentaiIndex  = "https://forums.e-hentai.org/index.php";
	public static readonly Url EHentaiBase   = "https://e-hentai.org/";
	public static readonly Url EHentaiLookup = "https://upld.e-hentai.org/image_lookup.php";

	public static readonly Url ExHentaiBase   = "https://exhentai.org/";
	public static readonly Url ExHentaiLookup = "https://upld.exhentai.org/upld/image_lookup.php";

#region

	private const string HOST_EH = ".e-hentai.org";
	private const string HOST_EX = ".exhentai.org";

#endregion

#endregion

#region Overrides of WebSearchEngine

	protected override async ValueTask<IEnumerable<SearchResultItem>> GetItems(IEnumerable<INode> n, SearchResult r)
	{
		
	}

#endregion

	public override void Dispose()
	{
		// m_client.Dispose();
		// m_clientHandler.Dispose();
		Jar.Clear();

		IsLoggedIn = false;
	}

	public event PropertyChangedEventHandler PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
			return false;

		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}

}

public sealed record EhResult : SearchResultItem, INodeResultParse<EhResult>
{

	public string Type { get; private set; }

	public string Pages { get; private set; }


	public string Author { get; private set; }

	public string AuthorUrl { get; private set; }


	public Dictionary<string, string[]> Tags { get; }

	private EhResult(SearchResult r) : base(r)
	{
		Tags = [];
	}

#region Implementation of IResultConverter2<out EhResult,out SearchResultItem>

	
	public static ValueTask<EhResult>  ParseResultItem(INode n, SearchResult sr)
	{
		// ReSharper disable InconsistentNaming
		var eh = new EhResult(sr);

		var gl1c = n.ChildNodes.FirstOrDefaultElementByClassName("gl1c");

		if (gl1c is { FirstChild: { } t1 }) {
			eh.Type = t1.TextContent;
		}

		var gl2c = n.ChildNodes.FirstOrDefaultElementByClassName("gl2c");

		if (gl2c is { }) {
			var cn = gl2c.RecurseChildren(1, 4);

			// var cn = gl2c.ChildNodes[1].ChildNodes[1].ChildNodes[1].ChildNodes[1];


			if (cn is { } div) {
				eh.Pages = div.TextContent;
			}
		}

		var gl3c = n.ChildNodes.FirstOrDefaultElementByClassName("gl3c glname");

		if (gl3c is { }) {
			if (gl3c.FirstChild is { } f) {
				eh.Url = f.TryGetAttribute(Serialization.Atr_href);

				if (f.FirstChild is { } ff) {
					eh.Title = ff.TextContent;
				}

				if (f.ChildNodes[1] is { ChildNodes: { Length: > 0 } cn } f2) {
					var tagValuesRaw = cn.Select(c => c.TryGetAttribute("title"));

					foreach (string s in tagValuesRaw) {
						if (s is not { }) {
							continue;
						}

						var split = s.Split(':');
						var tag   = split[0];
						var val   = split[1];

						if (eh.Tags.ContainsKey(tag)) {
							eh.Tags[tag].Add(val);
						}
						else {
							eh.Tags.TryAdd(tag, [val]);

						}
					}
				}
			}
		}

		var gl4c = n.ChildNodes.FirstOrDefaultElementByClassName("gl4c glhide");

		if (gl4c is { }) {
			if (gl4c.ChildNodes[0] is { FirstChild: { } div1 } div1Outer) {
				eh.AuthorUrl = div1.TryGetAttribute(Serialization.Atr_href);
				eh.Author    = div1Outer.TextContent ?? div1.TextContent;
			}

			if (gl4c.ChildNodes[1] is { } div2) {
				eh.Pages ??= div2.TextContent;
			}
		}


		if (eh.Tags.TryGetValue("artist", out var v)) {
			eh.Author = v.FirstOrDefault();
		}

		var sb = eh.Tags.Select(t => $"{t.Key}: {t.Value.QuickJoin()}").QuickJoin(" | ");

		eh.Description=sb;

		/*var gl1c        = n.ChildNodes[0];
		var gl2c        = n.ChildNodes[1];
		var ehx_compact = n.ChildNodes[2];
		var gl3c        = n.ChildNodes[3];
		var gl4c        = n.ChildNodes[4];*/

		return ValueTask.FromResult(eh);

	}

#endregion

}