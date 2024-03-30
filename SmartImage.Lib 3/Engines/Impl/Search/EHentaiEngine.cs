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
///     Handles both ExHentai and E-Hentai
/// </summary>
public sealed class EHentaiEngine : WebSearchEngine, IConfig, INotifyPropertyChanged
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

	#region

	private static readonly Url EHentaiIndex  = "https://forums.e-hentai.org/index.php";
	public static readonly  Url EHentaiBase   = "https://e-hentai.org/";
	private static readonly Url EHentaiLookup = "https://upld.e-hentai.org/image_lookup.php";

	public static readonly  Url ExHentaiBase   = "https://exhentai.org/";
	private static readonly Url ExHentaiLookup = "https://upld.exhentai.org/upld/image_lookup.php";

	#endregion

	static EHentaiEngine() { }

	public EHentaiEngine() : base(EHentaiBase)
	{
		m_client   = new HttpClient(m_clientHandler);
		IsLoggedIn = false;
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

	public async ValueTask ApplyAsync(SearchConfig cfg)
	{
		/*if (this is { IsLoggedIn: true }/* && !(Username != cfg.EhUsername && Password != cfg.EhPassword)#1#) {
			Debug.WriteLine($"{Name} is already logged in", nameof(ApplyAsync));

			return;
		}*/

		if (IsLoggedIn) {

			// throw new ArgumentException($"{Name} : username/password is null");
			return;

		}

		if (cfg.ReadCookies) {
			var ok = await LoginAsync();
			Debug.WriteLine($"{Name} logged in - {ok}", nameof(ApplyAsync));

		}
	}

	/*
	 * Default result layout is [Compact]
	 */
	public async Task<bool> LoginAsync(bool useEx = false)
	{
		/*
		if (IsLoggedIn) {
			return false;
		}
		*/

		var b = await CookiesManager.LoadCookiesAsync();

		if (!b) {
			return false;
		}

		var fcc = CookiesManager.Cookies.OfType<FirefoxCookie>().Where(x =>
		{
			if (!useEx) {
				return x.Host.Contains(HOST_EH);
			}

			return x.Host.Contains(HOST_EX);
		});

		/*var fcc = CookiesManager.Cookies.Where(x =>
		{
			if (!useEx) {
				return x.Domain.Contains(HOST_EH);
			}

			return x.Domain.Contains(HOST_EX);
		});*/

		/*var content = new MultipartFormDataContent()
		{
			{ new StringContent("1"), "CookieDate" },
			{ new StringContent("d"), "b" },
			{ new StringContent("1-6"), "bt" },
			{ new StringContent(Username), "UserName" },
			{ new StringContent(Password), "PassWord" },
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
						   .PostAsync(content);*/

		foreach (var cookie in fcc) {
			m_clientHandler.CookieContainer.Add(cookie.AsCookie());
		}

		// foreach (var fc in fcc) { }

		var res2 = await GetSessionAsync();

		return IsLoggedIn = res2.ResponseMessage.IsSuccessStatusCode;
	}

	public event PropertyChangedEventHandler PropertyChanged;

	protected override async Task<IDocument> GetDocumentAsync(SearchResult sr, SearchQuery query,
	                                                          CancellationToken token = default)
	{
		const string name = "a.jpg";
		string       t    = null;

		if (query.HasFile) {
			t = query.FilePath;

			if (Path.GetFileName(t) != name) {
				// Debugger.Break();
			}
		}
		else {
			var ok = query.LoadFile(name);

			if (ok) {
				t = query.FilePath;
			}
			else {
				Debugger.Break();
			}
		}

		if (t != null) {
			Trace.WriteLine($"allocated {t}", nameof(GetDocumentAsync));
		}

		var data = new MultipartFormDataContent()
		{
			{ new FileContent(t), "sfile", name },

			// { new StreamContent((Stream) query.Uni.Stream), "sfile", "a.jpg" },
			{ new StringContent("fs_similar") },
			{ new StringContent("fs_covers") },
			{ new StringContent("fs_exp") },
			{ new StringContent("fs_sfile") }
		};

		// data.Add(new FileContent(f.FullName), "sfile", "a.jpg");

		//todo
		/*m_clientHandler.CookieContainer.Add(Cookies);
		
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

		var content = await res.Content.ReadAsStringAsync(token);

		sr.RawUrl = res.RequestMessage.RequestUri;*/

		var req = new FlurlRequest(LookupUrl)
		{
			Content = data, 
			Headers =
			{
				{ "User-Agent", HttpUtilities.UserAgent }
			},
			Verb = HttpMethod.Post,
			
		};

		var res     = await Client.SendAsync(req, cancellationToken: token);
		var content = await res.GetStringAsync();
		
		sr.RawUrl = res.ResponseMessage.RequestMessage.RequestUri;

		if (content.Contains("Please wait a bit longer between each file search.")) {
			Debug.WriteLine($"cooldown", Name);
			sr.Status = SearchResultStatus.Cooldown;
			return null;
		}

		var parser = new HtmlParser();
		return await parser.ParseDocumentAsync(content, token);
	}

	private Task<IFlurlResponse> GetSessionAsync()
	{
		return ExHentaiBase.WithCookies(m_clientHandler.CookieContainer)
			.WithHeaders(new
			{
				User_Agent = HttpUtilities.UserAgent
			})
			.WithAutoRedirect(true)
			.GetAsync();
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

	private sealed record EhResult : IParseable<EhResult, INode>
	{

		internal string Type { get; set; }

		internal string Pages { get; set; }

		internal string Title { get; set; }

		internal string Author { get; set; }

		internal string AuthorUrl { get; set; }

		internal Url Url { get; set; }

		internal ConcurrentDictionary<string, ConcurrentBag<string>> Tags { get; } = new();

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

}