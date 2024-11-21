// Author: Deci | Project: SmartImage.Lib | Name: SearchServer.cs
// Date: 2024/11/21 @ 12:11:49

using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kantan.Net;
using Kantan.Net.Utilities;
using SmartImage.Lib.Images;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib;

using RouteCallbackMap = Dictionary<string, SmartHttpListener.HandleRequestCallback>;

public class SearchServer
{

	public static readonly JsonSerializerOptions Options2 = new (HttpUtilities.Options)
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	
	};

	public SearchClient Client { get; }

	private readonly SmartHttpListener m_server;

	private readonly RouteCallbackMap m_handlers;


	public SearchServer(SearchClient client, int port)
	{
		Client = client;

		m_handlers = new RouteCallbackMap()
		{
			["search"] = HandleRequestAsync
		};

		m_server = new SmartHttpListener(m_handlers, port);
	}

	private async Task<object> HandleRequestAsync(HttpListenerRequest b, HttpListenerResponse response)
	{
		object ok;

		try {
			var sz = await b.ReadRequestStringAsync();

			if (String.IsNullOrWhiteSpace(sz)) {
				return R1.Err_Query;
			}

			Debug.WriteLine($"{sz}");

			var sq = await SearchQuery.TryCreateAsync(sz);

			if (sq == SearchQuery.Null) {
				return R1.Err_Query;
			}

			var url = await sq.UploadAsync();

			var results = await Client.RunSearchAsync(sq);

			var allResults = results.SelectMany(x => x.Results).ToArray();
			// var ok1        = await response.WriteResponseJsonAsync(allResults);

			var json = JsonSerializer.Serialize(allResults, Options2);

			ok = await response.WriteResponseStringAsync(json);
		}
		catch (IOException io) {
			Trace.WriteLine($"{io}");
			ok = false;

		}

		return ok;
	}

	public Task StartAsync(CancellationToken ct = default)
	{
		return m_server.StartAsync(ct);
	}

}