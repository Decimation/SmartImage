// Author: Deci | Project: SmartImage.Lib | Name: OllamaModel.cs
// Date: 2025/07/15 @ 23:07:47

using System.Diagnostics.CodeAnalysis;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Utilities;
using SmartImage.Lib.Utilities.Diagnostics;

namespace SmartImage.Lib.Clients;
// TODO
#if EXPERIMENTAL

[Experimental(AppSupport.DIAG_ID_EXPERIMENTAL)]
public class OllamaClient
{

	protected static IFlurlClient Client { get; }

	protected static readonly ILogger Logger = AppSupport.Factory.CreateLogger(nameof(OllamaClient));

	static OllamaClient()
	{
		Client = (FlurlClient) FlurlHttp.Clients.GetOrAdd(nameof(OllamaClient), "http://localhost:11434", static builder =>
		{

			// builder.Headers.AddOrReplace(HeaderNames.UserAgent, HttpUtilities.UserAgent);

			// builder.Settings.JsonSerializer = new DefaultJsonSerializer();

			builder.Settings.AllowedHttpStatusRange = "*";

			builder.OnError(static f =>
			{
				// Debugger.Break();
				Logger.LogError(f.Exception, "Request: {Req}", f.Request);
			});

			builder.AddMiddleware(static () => new HttpLoggingHandler(Logger));

		});
	}

	public OllamaClient() { }

	public class OllamaRequest
	{

		public string Model { get; set; }

		public string Prompt { get; set; }

		public List<string> Images { get; set; }

		public string Format { get; set; }

		public bool Stream { get; set; }

		public OllamaRequest() { }

	}

	public Task<IFlurlResponse> CreateRequestAsync(SearchQuery query, CancellationToken ct = default)
	{
		var b64   = Convert.ToBase64String(query.AllocImage.Source);

		var ollamaRequest = new OllamaRequest()
		{
			Model  = "gemma3",
			Images = [b64],
			Prompt = "Find the source of this image. Respond using JSON",
			Format = "json",
			Stream = false
		};

		var req = Client.Request("api/generate")
			.PostJsonAsync(ollamaRequest, cancellationToken: ct);

		return req;
	}

}
#endif
