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
using SmartImage.Lib.Engines;
using SmartImage.Lib.Results;
using Spectre.Console.Rendering;
using System.Threading.Tasks;
using Kantan.Text;

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

	private async Task InitConfigAsync([CBN] object c)
	{
		//todo

		Client.Config.SearchEngines   = m_scs.SearchEngines;
		Client.Config.PriorityEngines = m_scs.PriorityEngines;

		Client.Config.ReadCookies = m_scs.ReadCookies;

		Client.Config.FlareSolverr       = m_scs.FlareSolverr;
		Client.Config.FlareSolverrApiUrl = m_scs.FlareSolverrApiUrl;

		await Client.LoadEnginesAsync();

	}

	public override async Task<int> ExecuteAsync(CommandContext context, ServerCommandSettings settings)
	{
		m_scs = settings;

		var uriPrefix = $"http://*:{m_scs.Port}/";
		Trace.WriteLine($"{uriPrefix}");

		await AnsiConsole.Progress().StartAsync(async ctx =>
		{
			var task = ctx.AddTask("Starting server");
			task.IsIndeterminate = true;
			// task.Description     = "Initializing config";
			await InitConfigAsync(null);
			task.Increment(ConsoleFormat.COMPLETE);
		});

		Listener = new SmartHttpListener(Handlers, uriPrefix);


		AnsiConsole.WriteLine($"Listening on {Listener.Listener.Prefixes.QuickJoin()}");

		await Listener.StartAsync();

		return BaseOSIntegration.EC_OK;
	}


	private async Task<object> HandleRequestAsync(HttpListenerRequest request, HttpListenerResponse response)
	{
		// AnsiConsole.Clear();

		object ok;
		var    remEndpoint = request.RemoteEndPoint;

		AnsiConsole.WriteLine($"Received request {remEndpoint}");
		
		// Trace.WriteLine($"Request endpoint: {remEndpoint}");

		var redirHdr    = request.Headers["Redirect"];
		var srvResponse = new SearchServerResponse();

		try {
			SearchQuery query = await GetQuery(request);

			if (query == SearchQuery.Null) {
				srvResponse.Message = R1.Err_Query;
			}
			else {
				var url = await query.UploadAsync();

				await Client.LoadEnginesAsync();

				var layout = new Layout("Root")
					.SplitColumns(new Layout("Left")
						              .SplitRows(new Layout("LT"), new Layout("LB")),
					              new Layout("Right"));

				// LT

				var grid    = ConsoleFormat.CreateConfigGrid(Client.Config, query);
				var padding = new Padding(vertical: 1, horizontal: 0);

				var gridPanel = new Panel(grid)
				{
					Padding = padding,
					Expand  = false
				};

				layout["LT"].Update(gridPanel);

				// LB

				var (engineMap, table) = ConsoleFormat.GetEngineMapTable(Client.Engines);
				table.Expand           = true;

				layout["LB"].Update(table);

				// Right

				var canvasImage = ConsoleFormat.GetQueryCanvasImage(query.Source);

				var canvasImagePanel = new Panel(canvasImage)
				{
					Padding = null
				};
				layout["Right"].Update(canvasImagePanel);

				var results = new ConcurrentBag<SearchResult>();

				AnsiConsole.Write(layout);

				await AnsiConsole.Live(layout).StartAsync(async ctx =>
				{
					var search = Client.RunSearchAsync(query);

					while (await Client.ResultChannel.Reader.WaitToReadAsync()) {
						var result = await Client.ResultChannel.Reader.ReadAsync();

						results.Add(result);
						var b = engineMap.TryGetValue(result.Engine, out int r);

						if (b) {
							table.Rows.Update(r, 1, new Text(result.Results.Count.ToString()));
							table.Rows.Update(r, 2, new Text(result.Status.ToString()));

							// table.Rows.RemoveAt(r);
							ctx.Refresh();
						}
					}

					await search;
					srvResponse.Results = results.ToArray();
					srvResponse.Best    = SearchClient.GetBest(srvResponse.Results);
				});

				engineMap.Clear();

			}


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

	private static async Task<SearchQuery> GetQuery(HttpListenerRequest request)
	{
		var contentType = request.Headers["Content-Type"] ?? MediaTypeNames.Text.Plain;

		Debug.WriteLine($"{contentType}");

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