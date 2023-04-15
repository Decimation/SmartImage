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
using SmartImage.Lib.Model;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Engines.Impl.Search;

/// <summary>
/// <see cref="SearchEngineOptions.EHentai"/>
/// Handles both ExHentai and E-Hentai
/// </summary>
public sealed class EHentaiEngine : WebSearchEngine, ILoginEngine, IConfig,
	INotifyPropertyChanged
{
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

	private string m_password;
	private string m_username;

	#region Url

	private static readonly Url EHentaiIndex = "https://forums.e-hentai.org/index.php";

	public static readonly Url ExHentaiBase = "https://exhentai.org/";
	public static readonly Url EHentaiBase  = "https://e-hentai.org/";

	private static readonly Url ExHentaiLookup = Url.Combine(ExHentaiBase, "upld", "image_lookup.php");
	private static readonly Url EHentaiLookup  = "https://upld.e-hentai.org/image_lookup.php";

	public override Url BaseUrl => IsLoggedIn ? ExHentaiBase : EHentaiBase;

	private Url LookupUrl => IsLoggedIn ? ExHentaiLookup : EHentaiLookup;

	#endregion

	public CookieJar Cookies { get; }

	public override SearchEngineOptions EngineOption => SearchEngineOptions.EHentai;

	protected override string NodesSelector => Serialization.S_EHentai;

	static EHentaiEngine() { }

	public EHentaiEngine() : base(EHentaiBase)
	{
		m_client   = new HttpClient(m_clientHandler);
		Cookies    = new CookieJar();
		IsLoggedIn = false;

		PropertyChanged += (sender, args) =>
		{
			if (IsLoggedIn) {
				IsLoggedIn = false;
			}

			Trace.WriteLine($"{IsLoggedIn} - {args.PropertyName}", nameof(PropertyChanged));
		};
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
		string u = null, p = null;

		if (this is { IsLoggedIn: true }) {
			Debug.WriteLine($"{Name} is already logged in", nameof(ApplyAsync));

			return;
		}

		u = cfg.EhUsername;
		p = cfg.EhPassword;

		if (string.IsNullOrWhiteSpace(u) || string.IsNullOrWhiteSpace(p)) {

			// throw new ArgumentException($"{Name} : username/password is null");
			return;

		}

		Username = u;
		Password = p;

		var ok = await LoginAsync();
		Debug.WriteLine($"{Name} logged in - {ok}", nameof(ApplyAsync));
	}

	protected override async Task<IDocument> GetDocumentAsync(object origin, SearchQuery query,
	                                                          CancellationToken? token = null)
	{
		const string name = "a.jpg";

		(string t, bool b) = await query.GetFilePathOrTempAsync(name);

		if (b) {
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

		foreach (var c in Cookies) {
			m_clientHandler.CookieContainer.Add(new Cookie(c.Name, c.Value, c.Path, c.Domain));
		}

		Debug.WriteLine($"{LookupUrl}", nameof(GetDocumentAsync));

		var req = new HttpRequestMessage(HttpMethod.Post, LookupUrl)
		{
			Content = data,
			Headers =
			{
				{ "User-Agent", HttpUtilities.UserAgent }
			},
		};

		var res = await m_client.SendAsync(req);

		var content = await res.Content.ReadAsStringAsync();

		if (content.Contains("Please wait a bit longer between each file search.")) {
			Debug.WriteLine($"cooldown", Name);
			return null;
		}

		var parser = new HtmlParser();
		return await parser.ParseDocumentAsync(content);
	}

	private Task<IFlurlResponse> GetSessionAsync()
	{
		return ExHentaiBase.WithCookies(Cookies)
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

		if (array.Any()) {
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

		item.Description = new[] { eh.Pages, $"({eh.Type})" }.QuickJoin(" ");
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
		Cookies.Clear();
		IsLoggedIn = false;
	}

	public string Username
	{
		get => m_username;
		set
		{
			if (value == m_username) return;
			m_username = value;
			OnPropertyChanged();
		}
	}

	public string Password
	{
		get => m_password;
		set
		{
			if (value == m_password) return;
			m_password = value;
			OnPropertyChanged();
		}
	}

	public bool IsLoggedIn { get; private set; }

	/*
	 * Default result layout is [Compact]
	 */

	public async Task<bool> LoginAsync()
	{
		if (Username is not { } || Password is not { }) {
			return false;
		}

		var content = new MultipartFormDataContent()
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
			               .PostAsync(content);

		foreach (FlurlCookie fc in cj) {
			Cookies.AddOrReplace(fc);
		}

		var res2 = await GetSessionAsync();

		return IsLoggedIn = res2.ResponseMessage.IsSuccessStatusCode;
	}

	#region

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

	public event PropertyChangedEventHandler PropertyChanged;

	#endregion

	private sealed record EhResult : IParseable<EhResult, INode>
	{
		internal string Type      { get; set; }
		internal string Pages     { get; set; }
		internal string Title     { get; set; }
		internal string Author    { get; set; }
		internal string AuthorUrl { get; set; }
		internal Url    Url       { get; set; }

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
								eh.Tags.TryAdd(tag, new() { val });

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