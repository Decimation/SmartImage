// Author: Deci | Project: SmartImage.Lib | Name: OllamaModel.cs
// Date: 2025/07/15 @ 23:07:47

using System.Text.Json;
using System.Text.Json.Nodes;
using Flurl.Http;
using Kantan.Net.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Images;
using SmartImage.Lib.Utilities;
using SmartImage.Lib.Utilities.Diagnostics;

namespace SmartImage.Lib.Engines;

public class OllamaModel
{

	protected static IFlurlClient Client { get; }

	protected static readonly ILogger Logger = AppSupport.Factory.CreateLogger(nameof(OllamaModel));

	static OllamaModel()
	{
		Client = (FlurlClient) FlurlHttp.Clients.GetOrAdd(nameof(OllamaModel), "http://localhost:11434", builder =>
		{

			// builder.Headers.AddOrReplace(HeaderNames.UserAgent, HttpUtilities.UserAgent);

			// builder.Settings.JsonSerializer = new DefaultJsonSerializer();

			builder.Settings.AllowedHttpStatusRange = "*";

			builder.OnError(f =>
			{
				// Debugger.Break();
				Logger.LogError(f.Exception, "Request: {Req}", f.Request);
			});

			builder.AddMiddleware(() => new HttpLoggingHandler(Logger));

		});
	}

	public OllamaModel() { }

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
		var bytes = query.Source.Image.ToBytes();
		var    b64     = Convert.ToBase64String(bytes);


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