// Author: Deci | Project: SmartImage.Rdx | Name: ServerCommand.cs
// Date: 2024/11/22 @ 03:11:26

using System.Collections.Concurrent;
using System.Diagnostics;
using HttpMultipartParser;
using System.Net.Mime;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Kantan.Net.Utilities;
using SmartImage.Lib;
using SmartImage.Lib.Utilities.Integration;
using SmartImage.Rdx.Shell;
using Spectre.Console;
using Spectre.Console.Cli;
using Kantan.Net;
using SmartImage.Lib.Utilities;
using System.Text.Json.Serialization;
using SmartImage.Lib.Results;
using Spectre.Console.Rendering;

#nullable disable
namespace SmartImage.Rdx.Commands;

using RouteCallbackMap = Dictionary<string, SmartHttpListener.HandleRequestCallback>;

#pragma warning disable IL2026

public sealed class ServerCommand : AsyncCommand<ServerCommandSettings>, IDisposable
{

	public SearchClient Client { get; }

	private ServerCommandSettings m_scs;

	public static readonly JsonSerializerOptions Options2 = new(HttpUtilities.Options)
	{
		WriteIndented          = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Converters =
		{
			new UrlTypeConverter(),
			new BaseSearchEngineTypeConverter(),

			// new SearchResultTypeConverter(),
		}

	};


	public SmartHttpListener Listener { get; private set; }

	public RouteCallbackMap Handlers { get; }

	public ServerCommand()
	{
		Client = new SearchClient(SearchConfig.Default);

		// Server = new SearchServer(Client, 25565);

		Handlers = new RouteCallbackMap()
		{
			["search"] = HandleRequestAsync,

		};


		m_scs = null;
	}

	public override async Task<int> ExecuteAsync(CommandContext context, ServerCommandSettings settings)
	{
		m_scs = settings;

		var uriPrefix = $"http://*:{m_scs.Port}/";
		Trace.WriteLine($"{uriPrefix}");
		Listener = new SmartHttpListener(Handlers, uriPrefix);

		AnsiConsole.WriteLine("Starting server");

		await Listener.StartAsync();

		return BaseOSIntegration.EC_OK;
	}

	private async Task<object> HandleRequestAsync(HttpListenerRequest request, HttpListenerResponse response)
	{
		// AnsiConsole.Clear();

		object ok;

		var redirHdr    = request.Headers["Redirect"];
		var srvResponse = new SearchServerResponse();

		try {
			var contentType = request.Headers["Content-Type"];
			Debug.WriteLine($"{contentType}");

			SearchQuery query;
			object      sqInput = null;

			var       mediaTypeHeaderValue = MediaTypeHeaderValue.Parse(contentType);
			using var sc                   = new StreamContent(request.InputStream);
			var       parser               = await MultipartFormDataParser.ParseAsync(request.InputStream);

			switch (mediaTypeHeaderValue.MediaType) {
				case MediaTypeNames.Text.Plain:
					goto default;

				case MediaTypeNames.Image.Bmp:
					break;

				case MediaTypeNames.Multipart.FormData:

					var file = parser.Files.FirstOrDefault();

					if (file == null) {
						srvResponse.Message = R1.Err_Content;
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

			if (query == SearchQuery.Null) {
				srvResponse.Message = R1.Err_Query;
			}
			else {
				var url = await query.UploadAsync();

				var layout = new Layout("Root")
					.SplitColumns(new Layout("Left"), 
					              new Layout("Right"));

				var grid = ConsoleFormat.CreateConfigGrid(Client.Config, query);
				var gridPanel = new Panel(grid) { Padding = null};
				layout["Left"].Update(gridPanel);

				var canvasImage = ConsoleFormat.GetQueryCanvasImage(query.Source);
				var canvasImagePanel = new Panel(canvasImage) { Padding = null};
				layout["Right"].Update(canvasImagePanel);

				var results = new ConcurrentBag<SearchResult>();
				AnsiConsole.Write(layout);
				await Client.LoadEnginesAsync();

				await AnsiConsole.Progress().StartAsync(async ctx =>
				{
					var task = ctx.AddTask("Searching", maxValue: Client.Engines.Length);

					// srvResponse.Results = await Client.RunSearchAsync(sq);

					var search = Client.RunSearchAsync(query);

					while (await Client.ResultChannel.Reader.WaitToReadAsync()) {
						var result = await Client.ResultChannel.Reader.ReadAsync();

						results.Add(result);

						// m_results.Add(result);


						/*var txt  = new Text(result.Engine.Name, GetEngineColor(result.Engine.EngineOption));
						var txt2 = new Text($"{result.Results.Count}");

						m_mainTable.AddRow(txt, txt2);*/


						task.Increment(1);
						ctx.Refresh();
					}

					await search;
					srvResponse.Results = results.ToArray();
					srvResponse.Best = SearchClient.GetBest(srvResponse.Results);

				});
			}


			/*
			var json = JsonSerializer.Serialize(allResults, Options2);
			ok = await response.WriteResponseStringAsync(json);
			*/

		}
		catch (IOException io) {
			Trace.WriteLine($"{io}");
		}
		finally {

			var responseStr   = JsonSerializer.Serialize(srvResponse, Options2);
			var responseBytes = Listener.Encoding.GetBytes(responseStr);
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

	public Task StartAsync(CancellationToken ct = default)
	{
		return Listener.StartAsync(ct);
	}

	public void Dispose()
	{
		Debug.WriteLine($"Disposing {nameof(ServerCommand)}");
		Client?.Dispose();
		Listener?.Dispose();
		Handlers.Clear();
	}

}
#pragma warning restore IL2026