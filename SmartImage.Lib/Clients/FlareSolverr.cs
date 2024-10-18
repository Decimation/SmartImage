// Author: Deci | Project: SmartImage.Lib | Name: FlareSolverr.cs
// Date: 2024/10/16 @ 15:10:40

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartImage.Lib.Clients;

public class FlareSolverr
{

	private static readonly SemaphoreLocker Locker = new SemaphoreLocker();
	private                 HttpClient      _httpClient;
	private readonly        Uri             _flareSolverrUri;

	public int    MaxTimeout    = 60000;
	public string ProxyUrl      = "";
	public string ProxyUsername = null;
	public string ProxyPassword = null;

	public FlareSolverr(string flareSolverrApiUrl)
	{
		var apiUrl = flareSolverrApiUrl;

		if (!apiUrl.EndsWith("/"))
			apiUrl += "/";
		_flareSolverrUri = new Uri(apiUrl + "v1");
	}

	public Task<FlareSolverrRoot> Solve(HttpRequestMessage request, string sessionId = "")
	{
		return SendFlareSolverrRequestAsync(GenerateFlareSolverrRequest(request, sessionId));
	}

	public Task<FlareSolverrRoot> CreateSession()
	{
		var req = new FlareSolverrRequest
		{
			Command    = "sessions.create",
			MaxTimeout = MaxTimeout,
			Proxy      = GetProxy()
		};
		return SendFlareSolverrRequestAsync(GetSolverRequestContent(req));
	}

	public Task<FlareSolverrRoot> ListSessionsAsync()
	{
		var req = new FlareSolverrRequest
		{
			Command    = "sessions.list",
			MaxTimeout = MaxTimeout,
			Proxy      = GetProxy()
		};
		return SendFlareSolverrRequestAsync(GetSolverRequestContent(req));
	}

	public async Task<FlareSolverrRoot> DestroySessionAsync(string sessionId)
	{
		var req = new FlareSolverrRequest
		{
			Command    = "sessions.destroy",
			MaxTimeout = MaxTimeout,
			Proxy      = GetProxy(),
			Session    = sessionId
		};
		return await SendFlareSolverrRequestAsync(GetSolverRequestContent(req));
	}

	private async Task<FlareSolverrRoot> SendFlareSolverrRequestAsync(HttpContent flareSolverrRequest)
	{
		FlareSolverrRoot result = null;

		await Locker.LockAsync(async () =>
		{
			HttpResponseMessage response;

			try {
				_httpClient = new HttpClient();

				// wait 5 more seconds to make sure we return the FlareSolverr timeout message
				_httpClient.Timeout = TimeSpan.FromMilliseconds(MaxTimeout + 5000);
				response            = await _httpClient.PostAsync(_flareSolverrUri, flareSolverrRequest);
			}
			catch (HttpRequestException e) {
				throw new FlareSolverrException("Error connecting to FlareSolverr server: " + e);
			}
			catch (Exception e) {
				throw new FlareSolverrException("Exception: " + e);
			}
			finally {
				_httpClient.Dispose();
			}

			// Don't try parsing if FlareSolverr hasn't returned 200 or 500
			if (response.StatusCode    != HttpStatusCode.OK
			    && response.StatusCode != HttpStatusCode.InternalServerError) {
				throw new FlareSolverrException("HTTP StatusCode not 200 or 500. Status is :"
				                                + response.StatusCode);
			}

			var resContent = await response.Content.ReadAsStringAsync();

			try {
				result = JsonSerializer.Deserialize<FlareSolverrRoot>(resContent, FlareSolverrHandler.s_jsonSerializerOptions);
			}
			catch (Exception) {
				throw new FlareSolverrException("Error parsing response, check FlareSolverr. Response: "
				                                + resContent);
			}

			try {
				Enum.TryParse(result.Status, true, out FlareSolverrStatusCode returnStatusCode);

				if (returnStatusCode.Equals(FlareSolverrStatusCode.ok)) {
					return result;
				}

				if (returnStatusCode.Equals(FlareSolverrStatusCode.warning)) {
					throw new FlareSolverrException(
						"FlareSolverr was able to process the request, but a captcha was detected. Message: "
						+ result.Message);
				}

				if (returnStatusCode.Equals(FlareSolverrStatusCode.error)) {
					throw new FlareSolverrException(
						"FlareSolverr was unable to process the request, please check FlareSolverr logs. Message: "
						+ result.Message);
				}

				throw new FlareSolverrException("Unable to map FlareSolverr returned status code, received code: "
				                                + result.Status + ". Message: " + result.Message);
			}
			catch (ArgumentException) {
				throw new FlareSolverrException("Error parsing status code, check FlareSolverr log. Status: "
				                                + result.Status + ". Message: " + result.Message);
			}
		});

		return result;
	}

	private FlareSolverrRequestProxy GetProxy()
	{
		FlareSolverrRequestProxy proxy = null;

		if (!string.IsNullOrWhiteSpace(ProxyUrl)) {
			proxy = new FlareSolverrRequestProxy
			{
				Url = ProxyUrl,
			};

			if (!string.IsNullOrWhiteSpace(ProxyUsername)) {
				proxy.Username = ProxyUsername;
			}

			;

			if (!string.IsNullOrWhiteSpace(ProxyPassword)) {
				proxy.Password = ProxyPassword;
			}

			;
		}

		return proxy;
	}

	private static HttpContent GetSolverRequestContent(FlareSolverrRequest request)
	{
		var content = JsonContent.Create(request, options: FlareSolverrHandler.s_jsonSerializerOptions);
		// HttpContent content = new StringContent(payload, Encoding.UTF8, "application/json");
		return content;
	}

	private HttpContent GenerateFlareSolverrRequest(HttpRequestMessage request, string sessionId = "")
	{
		FlareSolverrRequest req;

		if (string.IsNullOrWhiteSpace(sessionId))
			sessionId = null;

		var url = request.RequestUri.ToString();

		FlareSolverrRequestProxy proxy = GetProxy();

		if (request.Method == HttpMethod.Get) {
			req = new FlareSolverrRequest
			{
				Command    = "request.get",
				Url        = url,
				MaxTimeout = MaxTimeout,
				Proxy      = proxy,
				Session    = sessionId
			};
		}
		else if (request.Method == HttpMethod.Post) {
			// request.Content.GetType() doesn't work well when encoding != utf-8
			var contentMediaType = request.Content.Headers.ContentType?.MediaType.ToLower() ?? "<null>";

			if (contentMediaType.Contains("application/x-www-form-urlencoded")) {
				req = new FlareSolverrRequest
				{
					Command    = "request.post",
					Url        = url,
					PostData   = request.Content.ReadAsStringAsync().Result,
					MaxTimeout = MaxTimeout,
					Proxy      = proxy,
					Session    = sessionId
				};
			}
			else if (contentMediaType.Contains("multipart/form-data")
			         || contentMediaType.Contains("text/html")) {
				//TODO Implement - check if we just need to pass the content-type with the relevant headers
				throw new FlareSolverrException("Unimplemented POST Content-Type: " + contentMediaType);
			}
			else {
				throw new FlareSolverrException("Unsupported POST Content-Type: " + contentMediaType);
			}
		}
		else {
			throw new FlareSolverrException("Unsupported HttpMethod: " + request.Method);
		}

		return GetSolverRequestContent(req);
	}

}