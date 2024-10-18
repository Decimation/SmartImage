// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Argon;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Content;
using Kantan.Net.Web;
using SmartImage.Lib.Images;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SmartImage.Lib.Clients;

public class FlareSolverrHandler : DelegatingHandler
{

	public const string CMD_REQUEST_GET  = "request.get";
	public const string CMD_REQUEST_POST = "request.post";

	public const int DEFAULT_MAX_TIMEOUT = 60000;

	internal static readonly JsonSerializerOptions s_jsonSerializerOptions = new(JsonSerializerOptions.Default)
	{
		DefaultIgnoreCondition =
			JsonIgnoreCondition.WhenWritingDefault,
		PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower
	};

	internal static readonly DefaultJsonSerializer s_settingsJsonSerializer = new(s_jsonSerializerOptions)
		{ };

	// TODO: FlareSolverrSharp doesn't work 10/8/24


	public string BaseUrl { get; }

	private          FlareSolverr _flareSolverr;
	private readonly HttpClient   _client;
	private          string       _userAgent;

	/// <summary>
	/// Max timeout to solve the challenge.
	/// </summary>
	public int MaxTimeout = 60000;

	/// <summary>
	/// HTTP Proxy URL.
	/// Example: http://127.0.0.1:8888
	/// </summary>
	public string ProxyUrl = "";

	/// <summary>
	/// HTTP Proxy Username.
	/// </summary>
	public string ProxyUsername = null;

	/// <summary>
	/// HTTP Proxy Password.
	/// </summary>
	public string ProxyPassword = null;

	private HttpClientHandler HttpClientHandler => InnerHandler.GetMostInnerHandler() as HttpClientHandler;

	public FlareSolverrHandler(string baseUrl="http://localhost:8191/") : base(new HttpClientHandler())
	{
		BaseUrl = baseUrl;

		_client = new HttpClient(new HttpClientHandler
		{
			AllowAutoRedirect      = false,
			AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
			CookieContainer        = new CookieContainer()
		});
	}

	/*protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var x = SendAsync(request, cancellationToken);
		x.Wait(cancellationToken);
		return x.Result;
	}

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
	                                                             CancellationToken cancellationToken)
	{
		var cmd = (request.Method == HttpMethod.Get ? CMD_REQUEST_GET : CMD_REQUEST_POST);

		var flareSolverrReq = new HttpRequestMessage(HttpMethod.Post, BaseUrl);

		var flareSolverrReqMsg = new FlareSolverrRequest()
		{
			Url        = request.RequestUri.ToString(),
			Command    = cmd,
			MaxTimeout = DEFAULT_MAX_TIMEOUT
		};

		flareSolverrReq.Content = JsonContent.Create(flareSolverrReqMsg, options: s_jsonSerializerOptions);

		var flareSolverrRes = await base.SendAsync(flareSolverrReq, cancellationToken);

		var flareSolverrResStream = await flareSolverrRes.Content.ReadAsStreamAsync(cancellationToken);

		var flareSolverrRoot = JsonSerializer.Deserialize<FlareSolverrRoot>(flareSolverrResStream,
		                                                                    s_jsonSerializerOptions);

		var response = new HttpResponseMessage((HttpStatusCode) flareSolverrRoot.Solution.Status)
		{

			Headers =
			{

				Date = DateTimeOffset.Parse(flareSolverrRoot.Solution.Headers.Date)
			},
			RequestMessage = flareSolverrReq,

		};

		return flareSolverrRes;
	}
	*/


	private void SetUserAgentHeader(HttpRequestMessage request)
	{
		if (_userAgent != null) {
			// Overwrite the header
			request.Headers.Remove(HttpHeaders.UserAgent);
			request.Headers.Add(HttpHeaders.UserAgent, _userAgent);
		}
	}

	/// <summary>
	/// Sends an HTTP request to the inner handler to send to the server as an asynchronous operation.
	/// </summary>
	/// <param name="request">The HTTP request message to send to the server.</param>
	/// <param name="cancellationToken">A cancellation token to cancel operation.</param>
	/// <returns>The task object representing the asynchronous operation.</returns>
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
	                                                             CancellationToken cancellationToken)
	{
		// Init FlareSolverr
		if (_flareSolverr == null && !string.IsNullOrWhiteSpace(BaseUrl)) {
			_flareSolverr = new FlareSolverr(BaseUrl)
			{
				MaxTimeout    = MaxTimeout,
				ProxyUrl      = ProxyUrl,
				ProxyUsername = ProxyUsername,
				ProxyPassword = ProxyPassword
			};
		}

		// Set the User-Agent if required
		SetUserAgentHeader(request);

		// Perform the original user request
		var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

		// Detect if there is a challenge in the response
		if (ChallengeDetector.IsClearanceRequired(response)) {
			if (_flareSolverr == null)
				throw new FlareSolverrException("Challenge detected but FlareSolverr is not configured");

			// Resolve the challenge using FlareSolverr API
			var flareSolverrResponse = await _flareSolverr.Solve(request);

			// Save the FlareSolverr User-Agent for the following requests
			var flareSolverUserAgent = flareSolverrResponse.Solution.UserAgent;

			if (flareSolverUserAgent != null && !flareSolverUserAgent.Equals(request.Headers.UserAgent.ToString())) {
				_userAgent = flareSolverUserAgent;

				// Set the User-Agent if required
				SetUserAgentHeader(request);
			}

			// Change the cookies in the original request with the cookies provided by FlareSolverr
			InjectCookies(request, flareSolverrResponse);
			response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

			// Detect if there is a challenge in the response
			/*if (ChallengeDetector.IsClearanceRequired(response))
				throw new FlareSolverrException("The cookies provided by FlareSolverr are not valid");*/

			// Add the "Set-Cookie" header in the response with the cookies provided by FlareSolverr
			InjectSetCookieHeader(response, flareSolverrResponse);
		}

		return response;
	}

	public static class HttpHeaders
	{

		public const string UserAgent = "User-Agent";

		public const string Cookie = "Cookie";

		public const string SetCookie = "Set-Cookie";

	}

	private void InjectCookies(HttpRequestMessage request, FlareSolverrRoot flareSolverrResponse)
	{
		// use only Cloudflare and DDoS-GUARD cookies
		var flareCookies = flareSolverrResponse.Solution.Cookies
			.Where(cookie => IsCloudflareCookie(cookie.Name))
			.ToList();

		// not using cookies, just add flaresolverr cookies to the header request
		if (!HttpClientHandler.UseCookies) {
			foreach (var rCookie in flareCookies)
				request.Headers.Add(HttpHeaders.Cookie, rCookie.ToHeaderValue());

			return;
		}

		var currentCookies = HttpClientHandler.CookieContainer.GetCookies(request.RequestUri);

		// remove previous FlareSolverr cookies
		foreach (var cookie in flareCookies.Select(flareCookie => currentCookies[flareCookie.Name])
			         .Where(cookie => cookie != null))
			cookie.Expired = true;

		// add FlareSolverr cookies to CookieContainer
		foreach (var rCookie in flareCookies)
			HttpClientHandler.CookieContainer.Add(request.RequestUri, rCookie.AsCookie());

		// check if there is too many cookies, we may need to remove some
		if (HttpClientHandler.CookieContainer.PerDomainCapacity >= currentCookies.Count)
			return;

		// check if indeed we have too many cookies
		var validCookiesCount = currentCookies.Cast<Cookie>().Count(cookie => !cookie.Expired);

		if (HttpClientHandler.CookieContainer.PerDomainCapacity >= validCookiesCount)
			return;

		// if there is a too many cookies, we have to make space
		// maybe is better to raise an exception?
		var cookieExcess = HttpClientHandler.CookieContainer.PerDomainCapacity - validCookiesCount;

		foreach (Cookie cookie in currentCookies) {
			if (cookieExcess == 0)
				break;

			if (cookie.Expired || IsCloudflareCookie(cookie.Name))
				continue;

			cookie.Expired =  true;
			cookieExcess   -= 1;
		}
	}

	private static void InjectSetCookieHeader(HttpResponseMessage response, FlareSolverrRoot flareSolverrResponse)
	{
		// inject set-cookie headers in the response
		foreach (var rCookie in flareSolverrResponse.Solution.Cookies.Where(cookie => IsCloudflareCookie(cookie.Name)))
			response.Headers.Add(HttpHeaders.SetCookie, rCookie.ToHeaderValue());
	}

	private static bool IsCloudflareCookie(string cookieName)
		=> cookieName.StartsWith("cf_") || cookieName.StartsWith("__cf") || cookieName.StartsWith("__ddg");

}

public class SemaphoreLocker
{

	private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

	public async Task LockAsync<T>(Func<T> worker)
		where T : Task
	{
		await _semaphore.WaitAsync();

		try {
			await worker();
		}
		finally {
			_semaphore.Release();
		}
	}

}

/// <summary>
/// The exception that is thrown if FlareSolverr fails
/// </summary>
public class FlareSolverrException : HttpRequestException
{

	public FlareSolverrException(string message) : base(message) { }

}

public enum FlareSolverrStatusCode
{

	ok,
	warning,
	error

}

#region API Objects

public class FlareSolverrRequestProxy
{

	[JsonProperty("url")]
	public string Url;

	[JsonProperty("username")]
	public string Username;

	[JsonProperty("password")]
	public string Password;

}

public record FlareSolverrRequest
{

	// todo

	[JsonPropertyName("cmd")]
	public string Command { get; set; }

	public List<FlareSolverrCookie> Cookies { get; set; }

	public int MaxTimeout { get; set; }

	public FlareSolverrRequestProxy Proxy { get; set; } //todo

	public string Session { get; set; }

	[JsonPropertyName("session_ttl_minutes")]
	public int SessionTtl { get; set; }

	public string Url { get; set; }

	public string PostData { get; set; }

	public bool ReturnOnlyCookies { get; set; }


	public FlareSolverrRequest() { }

}

public class FlareSolverrCookie : IBrowserCookie
{

	[JsonPropertyName("name")]
	public string Name { get; set; }

	[JsonPropertyName("value")]
	public string Value { get; set; }

	[JsonPropertyName("domain")]
	public string Domain { get; set; }

	[JsonPropertyName("path")]
	public string Path { get; set; }

	[JsonPropertyName("expires")]
	public double Expires { get; set; }

	[JsonPropertyName("size")]
	public int Size { get; set; }

	[JsonPropertyName("httpOnly")]
	public bool HttpOnly { get; set; }

	[JsonPropertyName("secure")]
	public bool Secure { get; set; }

	[JsonPropertyName("session")]
	public bool Session { get; set; }

	[JsonPropertyName("sameSite")]
	public string SameSite { get; set; }

	public Cookie AsCookie()
	{
		return new Cookie(Name, Value, Path, Domain)
		{
			Secure   = Secure,
			HttpOnly = HttpOnly,
		};
	}

	public FlurlCookie AsFlurlCookie()
	{
		return new FlurlCookie(Name, Value)
		{
			Domain   = Domain,
			HttpOnly = HttpOnly,
			Path     = Path,
			SameSite = Enum.Parse<SameSite>(SameSite),
			Secure   = Secure
		};
	}


	public string ToHeaderValue()
		=> $"{Name}={Value}";

}

public class FlareSolverrHeaders
{

	[JsonPropertyName("status")]
	public string Status { get; set; }

	[JsonPropertyName("date")]
	public string Date { get; set; }

	[JsonPropertyName("expires")]
	public string Expires { get; set; }

	[JsonPropertyName("cache-control")]
	public string CacheControl { get; set; }

	[JsonPropertyName("content-type")]
	public string ContentType { get; set; }

	[JsonPropertyName("strict-transport-security")]
	public string StrictTransportSecurity { get; set; }

	[JsonPropertyName("p3p")]
	public string P3p { get; set; }

	[JsonPropertyName("content-encoding")]
	public string ContentEncoding { get; set; }

	[JsonPropertyName("server")]
	public string Server { get; set; }

	[JsonPropertyName("content-length")]
	public string ContentLength { get; set; }

	[JsonPropertyName("x-xss-protection")]
	public string XXssProtection { get; set; }

	[JsonPropertyName("x-frame-options")]
	public string XFrameOptions { get; set; }

	[JsonPropertyName("set-cookie")]
	public string SetCookie { get; set; }

}

public class FlareSolverrRoot
{

	[JsonPropertyName("solution")]
	public FlareSolverrSolution Solution { get; set; }

	[JsonPropertyName("status")]
	public string Status { get; set; }

	[JsonPropertyName("message")]
	public string Message { get; set; }

	[JsonPropertyName("startTimestamp")]
	public long StartTimestamp { get; set; }

	[JsonPropertyName("endTimestamp")]
	public long EndTimestamp { get; set; }

	[JsonPropertyName("version")]
	public string Version { get; set; }

}

public class FlareSolverrSolution
{

	[JsonPropertyName("url")]
	public string Url { get; set; }

	[JsonPropertyName("status")]
	public int Status { get; set; }

	[JsonPropertyName("headers")]
	public FlareSolverrHeaders Headers { get; set; }

	[JsonPropertyName("response")]
	public string Response { get; set; }

	[JsonPropertyName("cookies")]
	public List<FlareSolverrCookie> Cookies { get; set; }

	[JsonPropertyName("userAgent")]
	public string UserAgent { get; set; }

}

#endregion