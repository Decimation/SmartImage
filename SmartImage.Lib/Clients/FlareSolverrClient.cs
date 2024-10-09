// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);

using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl.Http;
using Flurl.Http.Configuration;

namespace SmartImage.Lib.Clients;

public class FlareSolverrClient : IDisposable
{

	public const string CMD_REQUEST_GET  = "request.get";
	public const string CMD_REQUEST_POST = "request.post";

	public const int DEFAULT_MAX_TIMEOUT = 60000;

	private static readonly JsonSerializerOptions s_jsonSerializerOptions = new(JsonSerializerOptions.Default)
	{
		DefaultIgnoreCondition =
			JsonIgnoreCondition.WhenWritingDefault
	};

	private static readonly DefaultJsonSerializer s_settingsJsonSerializer = new(s_jsonSerializerOptions)
		{ };

	// TODO: FlareSolverrSharp doesn't work 10/8/24

	public FlurlClient Client { get; }

	public string BaseUrl { get; }

	public FlareSolverrClient(string baseUrl)
	{
		BaseUrl = baseUrl;

		Client = (FlurlClient) FlurlHttp.Clients.GetOrAdd(nameof(FlareSolverrClient), BaseUrl, builder =>
		{
			builder.AllowAnyHttpStatus();

			builder.Settings.JsonSerializer = s_settingsJsonSerializer;
		});

	}

	public async ValueTask<bool> IsConnectedAsync(int maxTimeout = DEFAULT_MAX_TIMEOUT)
	{
		try {
			using var res = await SendAsync("https://nowsecure.nl/", CMD_REQUEST_GET, maxTimeout);

			if (res == null) {
				return false;
			}

			var obj = await res.GetJsonAsync<FlareSolverrRoot>();
			return obj.Status == "ok";
		}
		catch {
			return false;
		}
		finally { }


	}

	public static readonly FlareSolverrClient Instance = new("http://localhost:8191/v1");

	public Task<IFlurlResponse> SendAsync(FlareSolverrRequest request)
	{
		var resp = Client.Request()
			.PostJsonAsync(request);

		return resp;
	}

	public Task<IFlurlResponse> SendAsync(string url, string cmd, int maxTimeout = DEFAULT_MAX_TIMEOUT)
	{

		return SendAsync(new FlareSolverrRequest() { Url = url, Command = cmd, MaxTimeout = maxTimeout });

	}

	public void Dispose()
	{
		Client?.Dispose();
	}

}

#region API Objects

public record FlareSolverrRequest
{

	// todo

	[JsonPropertyName("cmd")]
	public string Command { get; set; }

	public List<FlareSolverrCookie> Cookies { get; set; }

	public int MaxTimeout { get; set; }

	public Dictionary<string, object> Proxy { get; set; } //todo

	public string Session { get; set; }

	[JsonPropertyName("session_ttl_minutes")]
	public int SessionTtl { get; set; }

	public string Url { get; set; }

	public string PostData { get; set; }

	public bool ReturnOnlyCookies { get; set; }


	public FlareSolverrRequest() { }

}

public class FlareSolverrCookie
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