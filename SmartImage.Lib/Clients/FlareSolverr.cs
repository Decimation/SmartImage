// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);

using System.Text.Json.Serialization;
using Flurl.Http;

namespace SmartImage.Lib.Clients;

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

public class FlareSolverrClient : IDisposable
{
	// TODO: FlareSolverrSharp doesn't work 10/8/24

	public FlurlClient Client { get; }

	public string Url { get; }

	public FlareSolverrClient(string url)
	{
		Url    = url;
		Client = new FlurlClient(Url);
	}

	public Task<IFlurlResponse> Get(string url, string cmd, int maxTimeout = 60000)
	{
		var body = new
		{
			cmd        = cmd,
			url        = url,
			maxTimeout = 60000
		};

		var resp = Client.Request()
			.PostJsonAsync(body);

		return resp;

	}

	public void Dispose()
	{
		Client?.Dispose();
	}

}