// Read S SmartImage.Lib EHentaiEngine.cs
// 2023-01-13 @ 11:21 PM

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Flurl.Http;
using Flurl.Http.Content;
using Kantan.Net;
using Kantan.Net.Utilities;
using Kantan.Net.Web;
using Kantan.Text;
using SmartImage.Lib.Model;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Engines.Impl.Search;

/// <summary>
///     <see cref="SearchEngineOptions.EHentai" />
/// </summary>
/// <remarks>Handles both ExHentai and E-Hentai</remarks>
public sealed class EHentaiEngine : WebSearchEngine, IConfig, ICookieEngine, INotifyPropertyChanged
{

	private const string HOST_EH = ".e-hentai.org";
	private const string HOST_EX = ".exhentai.org";

	private readonly HttpClient m_client;

	// NOTE: a separate HttpClient is used for EHentai because of special network requests and other unique requirements...

	private readonly HttpClientHandler m_clientHandler = new()
	{
		AllowAutoRedirect              = true,
		MaxAutomaticRedirections       = 15,
		CheckCertificateRevocationList = false,
		UseCookies                     = true,
		CookieContainer                = new() { },

	};

	public override Url BaseUrl => IsLoggedIn ? ExHentaiBase : EHentaiBase;

	private Url LookupUrl => IsLoggedIn ? ExHentaiLookup : EHentaiLookup;

	public override SearchEngineOptions EngineOption => SearchEngineOptions.EHentai;

	protected override string NodesSelector => Serialization.S_EHentai;

	public bool IsLoggedIn { get; private set; }

	private readonly CookieCollection m_cookies;

	#region

	public static readonly Url EHentaiIndex  = "https://forums.e-hentai.org/index.php";
	public static readonly Url EHentaiBase   = "https://e-hentai.org/";
	public static readonly Url EHentaiLookup = "https://upld.e-hentai.org/image_lookup.php";

	public static readonly Url ExHentaiBase   = "https://exhentai.org/";
	public static readonly Url ExHentaiLookup = "https://upld.exhentai.org/upld/image_lookup.php";

	#endregion

	static EHentaiEngine() { }

	public EHentaiEngine() : base(EHentaiBase)
	{
		m_client   = new HttpClient(m_clientHandler);
		IsLoggedIn = false;
		m_cookies  = new();
	}

	/*
	 * https://gitlab.com/NekoInverter/EhViewer/-/tree/master/app/src/main/java/com/hippo/ehviewer/client
	 * https://gitlab.com/NekoInverter/EhViewer/-/tree/master/app/src/main/java/com/hippo/ehviewer
	 * https://gitlab.com/NekoInverter/EhViewer/-/blob/master/app/src/main/java/com/hippo/ehviewer/client/EhUrl.java
	 * https://gitlab.com/NekoInverter/EhViewer/-/blob/master/app/src/main/java/com/hippo/ehviewer/client/EhEngine.java
	 * https://gitlab.com/NekoInverter/EhViewer/-/blob/master/app/src/main/java/com/hippo/ehviewer/EhApplication.java
	 * https://gitlab.com/NekoInverter/EhViewer/-/blob/master/app/src/main/java/com/hippo/ehviewer/client/data/ListUrlBuilder.java
	 * https://gitlab.com/NekoInverter/EhViewer/-/blob/master/app/src/main/java/com/hippo/ehviewer/client/EhCookieStore.java
	 */

	public async ValueTask ApplyConfigAsync(SearchConfig cfg)
	{
		/*if (this is { IsLoggedIn: true }/* && !(Username != cfg.EhUsername && Password != cfg.EhPassword)#1#) {
			Debug.WriteLine($"{Name} is already logged in", nameof(ApplyConfigAsync));

			return;
		}*/
		//
		return;
	}

	/*
	 * Default result layout is [Compact]
	 */

	public async ValueTask<bool> ApplyCookiesAsync(IEnumerable<IBrowserCookie> cookies = null)
	{

		Trace.WriteLine($"Applying cookies to {Name}");

		if (await CookiesManager.Instance.LoadCookiesAsync()) {
			cookies ??= CookiesManager.Instance.Cookies;
		}

		var fcc = cookies.OfType<FirefoxCookie>().Where(x =>
		{
			if (!true) {
				return x.Host.Contains(HOST_EH);
			}

			return x.Host.Contains(HOST_EX);
		});

		foreach (var cookie in fcc) {
			m_cookies.Add(cookie.AsCookie());
		}

		var res2 = await GetSessionAsync(true);

		/*var res2 = await EHentaiBase.WithCookies(m_clientHandler.CookieContainer)
			.WithHeaders(new
			{
				User_Agent = HttpUtilities.UserAgent
			})
			.WithAutoRedirect(true)
			.GetAsync();*/
		
		m_clientHandler.CookieContainer.Add(m_cookies);
		m_client.Timeout = Timeout;

		return IsLoggedIn = res2.ResponseMessage.IsSuccessStatusCode;
	}

	private Task<IFlurlResponse> GetSessionAsync(bool useEx = false)
	{
		return (useEx ? ExHentaiBase : EHentaiBase)
			.WithCookies(m_cookies)
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

		if (query.Image.HasFile) {
			filePath = query.Image.FilePath;
			fileName = Path.GetFileName(filePath);

			/*if (Path.GetFileName(t) != name) {
				// Debugger.Break();
			}*/
		}
		else {
			fileName = SFILE_NAME_DEFAULT;
			var ok = query.Image.TryGetFile(fileName);

			if (ok) {
				filePath = query.Image.FilePath;
			}
			else {
				Debugger.Break();
			}
		}

		if (filePath != null) {
			Trace.WriteLine($"allocated {filePath}", nameof(GetDocumentAsync));
		}

		var data = new MultipartFormDataContent()
		{
			{ new FileContent(filePath), "sfile", fileName },

			// { new StreamContent((Stream) query.Uni.Stream), "sfile", "a.jpg" },
			{ new StringContent("fs_similar") },
			{ new StringContent("fs_covers") },
			{ new StringContent("fs_exp") },
			{ new StringContent("fs_sfile") },
			{ new StringContent("dm_l"), "inline_set" }
		};

		// data.Add(new FileContent(f.FullName), "sfile", "a.jpg");

		//todo
		/*m_clientHandler.CookieContainer.Add(m_cookies);

		m_client.Timeout = Timeout;*/

		Debug.WriteLine($"{LookupUrl}", nameof(GetDocumentAsync));

		var req = new HttpRequestMessage(HttpMethod.Post, LookupUrl)
		{
			Content = data,
			Headers =
			{
				{ "User-Agent", HttpUtilities.UserAgent }
			},

		};

		var res = await m_client.SendAsync(req, token);

		// Debug.WriteLine($"{res.StatusCode}");

		sr.RawUrl = res.RequestMessage.RequestUri;

		// Debug.WriteLine($"{sr.RawUrl}");
		var content = await res.Content.ReadAsStringAsync(token);

		// var content2 = await sr.RawUrl.GetStringAsync(cancellationToken: token);

		if (content.Contains("Please wait a bit longer between each file search.")) {
			Debug.WriteLine($"cooldown", Name);
			sr.Status = SearchResultStatus.Cooldown;
			return null;
		}

		var parser = new HtmlParser();
		return await parser.ParseDocumentAsync(content, token);
	}

	protected override ValueTask<INode[]> GetNodes(IDocument d)
	{
		// Index 0 is table header
		var array = d.Body.SelectNodes(NodesSelector).ToArray();

		if (array.Length != 0) {
			array = array[1..];

		}

		return ValueTask.FromResult(array);
	}

	protected override ValueTask<SearchResultItem> ParseResultItem(INode n, SearchResult r)
	{
		var item = new SearchResultItem(r)
			{ };

		var eh = EhResult.Parse(n);

		if (eh.Tags.TryGetValue("artist", out var v)) {
			item.Artist = v.FirstOrDefault();
		}

		var sb = eh.Tags.Select(t => $"{t.Key}: {t.Value.QuickJoin()}").QuickJoin(" | ");
		item.Description = $"{eh.Pages} ({sb})";
		item.Title       = eh.Title;
		item.Url         = eh.Url;

		/*var gl1c        = n.ChildNodes[0];
		var gl2c        = n.ChildNodes[1];
		var ehx_compact = n.ChildNodes[2];
		var gl3c        = n.ChildNodes[3];
		var gl4c        = n.ChildNodes[4];*/

		return ValueTask.FromResult(item);
	}

	public override void Dispose()
	{
		m_client.Dispose();
		m_clientHandler.Dispose();

		IsLoggedIn = false;
	}

	public event PropertyChangedEventHandler PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value)) return false;

		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}

}

public sealed record EhResult : IParseable<EhResult, INode>
{

	public string Type { get; internal set; }

	public string Pages { get; internal set; }

	public string Title { get; internal set; }

	public string Author { get; internal set; }

	public string AuthorUrl { get; internal set; }

	public Url Url { get; internal set; }

	public ConcurrentDictionary<string, ConcurrentBag<string>> Tags { get; } = new();

	public static EhResult Parse(INode n)
	{
		// ReSharper disable InconsistentNaming
		var eh = new EhResult();

		var gl1c = n.ChildNodes.TryFindSingleElementByClassName("gl1c");

		if (gl1c is { }) {
			if (gl1c.FirstChild is { } t) {
				eh.Type = t.TextContent;
			}
		}

		var gl2c = n.ChildNodes.TryFindSingleElementByClassName("gl2c");

		if (gl2c is { }) {
			if (gl2c.ChildNodes[1].ChildNodes[1].ChildNodes[1].ChildNodes[1] is { } div) {
				eh.Pages = div.TextContent;
			}
		}

		var gl3c = n.ChildNodes.TryFindSingleElementByClassName("gl3c glname");

		if (gl3c is { }) {
			if (gl3c.FirstChild is { } f) {
				eh.Url = (Url) f.TryGetAttribute(Serialization.Atr_href);

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

		var gl4c = n.ChildNodes.TryFindSingleElementByClassName("gl4c glhide");

		if (gl4c is { }) {
			if (gl4c.ChildNodes[0] is { FirstChild: { } div1 } div1Outer) {
				eh.AuthorUrl = div1.TryGetAttribute(Serialization.Atr_href);
				eh.Author    = div1Outer.TextContent ?? div1.TextContent;
			}

			if (gl4c.ChildNodes[1] is { } div2) {
				eh.Pages ??= div2.TextContent;
			}
		}

		return eh;

		// ReSharper restore InconsistentNaming
	}

}