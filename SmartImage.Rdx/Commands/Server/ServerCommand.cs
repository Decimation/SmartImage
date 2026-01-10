// Author: Deci | Project: SmartImage.Rdx | Name: ServerCommand.cs
// Date: 2024/11/22 @ 03:11:26

#nullable disable
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HttpMultipartParser;
using Kantan.Net.Utilities;
using Kantan.Text;
using Microsoft.Extensions.Logging;
using SmartImage.Lib;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Utilities;
using SmartImage.Lib.Utilities.Integration;
using SmartImage.Rdx.Commands.Common;
using SmartImage.Rdx.Shell;
using Spectre.Console.Cli;

// TODO: DEPRECATE; HAVEN'T WORKED ON THIS IN AGES

namespace SmartImage.Rdx.Commands.Server;

using RouteCallbackMap = Dictionary<string, ServerCommand.HandleRequestCallback2>;

#pragma warning disable IL2026

public sealed class ServerCommand : CommonAsyncCommand<ServerCommandSettings>
{

	public SearchClient Client { get; }

	private static readonly ILogger s_logger = AppSupport.Factory.CreateLogger(nameof(ServerCommand));

	public delegate Task<object> HandleRequestCallback2(HttpListenerContext ctx);

	public static readonly JsonSerializerOptions Options2 = new(HttpUtilities.Options)
	{
		WriteIndented          = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Converters =
		{
			new UrlTypeConverter(),

			// new SearchResultTypeConverter(),
		}

	};


	public HttpListener Listener { get; }

	private const int CHUNK_SIZE = 1024;

	public RouteCallbackMap Handlers { get; }

	public Encoding Encoding { get; internal set; }

	// public delegate Task<string> RequestDataCallback(byte[] buf);

	// public delegate Task<string> HandleRequestCallback(HttpListenerRequest buf);

	public SpcTable Shared { get; internal set; }

	public ServerCommand()
	{
		Config = new SearchConfig();
		Client = new SearchClient(Config);

		// Server = new SearchServer(Client, 25565);


		Handlers = new RouteCallbackMap()
		{
			["search"] = HandleRequestAsync2
		};


		Shared = Elements.GetEngineMapTableBase();

		Listener = new HttpListener()
		{
			TimeoutManager =
			{
				// IdleConnection = Timeout.InfiniteTimeSpan,
			},
		};


		// Start();

		// Debug.WriteLine("ProtoPad HTTP Server started");
	}


	public async Task StartAsync(CancellationToken ct = default)
	{
		if (!Listener.IsListening) {
			Listener.Start();

			// Listener.BeginGetContext(HandleRequest, Listener);

			while (Listener.IsListening) {
				var ctx = await Listener.GetContextAsync().ConfigureAwait(false);

				s_logger.LogTrace("{Context}", ctx);

				// var res = await HandleRequestAsync(ctx, ct);

				// var request  = ctx.Request;
				// var response = ctx.Response;

				foreach (var requestHandler in Handlers) {

					var requestUrl = ctx.Request.Url;

					if (requestUrl != null && !requestUrl.PathAndQuery.Contains(requestHandler.Key)) {
						continue;
					}

					/*var task = Task.Run(() =>
					{
						var req = requestHandler.Value(ctx);

						return req;
					}, ct);*/

					var task = requestHandler.Value(ctx);

					AnsiConsole.WriteLine($"Queued {task.Id}");
					var res = await task;

					AnsiConsole.WriteLine($"{task.Id} -> {res}");

					/*if (handlerObject is byte[] responseBytes) {
						//...
					}
					else if (handlerObject is string sz) {
						responseBytes = Encoding.GetBytes(sz);
					}
					else {
						responseBytes = await request.ReadRequestDataAsync(ct: ct);
					}*/


				}


				/*if (handlerObject is byte[] responseBytes) {
						//...
					}
					else if (handlerObject is string sz) {
						responseBytes = Encoding.GetBytes(sz);
					}
					else {
						responseBytes = await request.ReadRequestDataAsync(ct: ct);
					}*/


				if (ct.IsCancellationRequested) {
					break;
				}
			}
		}
	}

	// 5737aabe216331623bea509108a768d4796cae77

	private async Task<object> HandleRequestAsync2(HttpListenerContext ctx)
	{

		// AnsiConsole.Clear();
		var request  = ctx.Request;
		var response = ctx.Response;

		object ok;
		var    remEndpoint = request.RemoteEndPoint;

		// Trace.WriteLine($"Request endpoint: {remEndpoint}");

		var redirHdr    = request.Headers["Redirect"];
		var srvResponse = new SearchServerResponse();

		try {
			SearchQuery query = await GetQueryFromRequestAsync(request);

			if (query == null || query == SearchQuery.Null) {
				srvResponse.Message = R1.Err_Query;
			}
			else {
				var url = await query.TryUploadAsync();

				var results = new ConcurrentBag<SearchResult>();

				var search = Client.RunSearchAsync(query);

				while (await Client.ResultChannel.Reader.WaitToReadAsync()) {
					var result = await Client.ResultChannel.Reader.ReadAsync();

					results.Add(result);

				}

				await search;

				srvResponse.Results = results.ToArray();
				srvResponse.Best    = SearchClient.GetBest(srvResponse.Results);
			}


		}
		catch (IOException io) {
			Trace.WriteLine($"{io}");
		}
		finally {

			var responseStr   = JsonSerializer.Serialize(srvResponse, Options2);
			var responseBytes = Encoding.GetBytes(responseStr);
			var writeOk       = await response.WriteResponseDataAsync(responseBytes);

			ok = writeOk;

			if (!String.IsNullOrWhiteSpace(redirHdr)) {
				response.Redirect(srvResponse.Best.Url);
			}

			response.OutputStream.Close();
			response.Close();

		}

		return ok;
	}

	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, ServerCommandSettings settings, CancellationToken cancellationToken)
	{
		InitConfig(settings);

		var uriPrefix = $"http://*:{CommandSettings.Port}/";
		s_logger.LogTrace("Listening on {URI}", uriPrefix);

		Listener.Prefixes.Add(uriPrefix);
		Encoding = HttpUtilities.DefaultEncoding;

		AnsiConsole.WriteLine($"Listening on {Listener.Prefixes.QuickJoin()}");

		await StartAsync(cancellationToken);

		return BaseOSIntegration.EC_OK;
	}


	private static async Task<SearchQuery> GetQueryFromRequestAsync(HttpListenerRequest request)
	{
		var contentType = request.Headers["Content-Type"] ?? MediaTypeNames.Text.Plain;

		s_logger.LogDebug("{ContentType}", contentType);
		SearchQuery query;
		object      sqInput = null;

		// contentType??= MediaTypeNames.Multipart.FormData;

		var mediaTypeHeaderValue = MediaTypeHeaderValue.Parse(contentType);

		using var sc = new StreamContent(request.InputStream);

		switch (mediaTypeHeaderValue.MediaType) {
			case MediaTypeNames.Text.Plain:
				goto default;

			case MediaTypeNames.Image.Bmp:
				break;

			case MediaTypeNames.Multipart.FormData:
				var parser = await MultipartFormDataParser.ParseAsync(request.InputStream);

				var file = parser.Files.FirstOrDefault();

				if (file == null) {
					// srvResponse.Message = R1.Err_Content;
					return null;

					// sqInput = null;
				}
				else {
					string filename = file.FileName;
					Stream data     = file.Data;
					sqInput = data;

				}

				break;

			default:
				sqInput = await sc.ReadAsStringAsync();
				break;
		}

		query = await SearchQuery.TryCreateAsync(sqInput);
		return query;
	}

	public override void Dispose()
	{
		s_logger.LogDebug("Disposing server");
		Client?.Dispose();
		Handlers.Clear();
		Listener?.Close();
	}

}

#pragma warning restore IL2026