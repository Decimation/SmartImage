// Author: Deci | Project: SmartImage.Lib | Name: SearchServer.cs
// Date: 2024/11/21 @ 12:11:49

using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EmptyFiles;
using HttpMultipartParser;
using Kantan.Net;
using Kantan.Net.Utilities;
using Novus.Streams;
using SmartImage.Lib.Images;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib;

using RouteCallbackMap = Dictionary<string, SmartHttpListener.HandleRequestCallback>;

#pragma warning disable IL2026

public class SearchServer : IDisposable
{

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

	public SearchClient Client { get; }

	public SmartHttpListener Listener { get; }

	public RouteCallbackMap Handlers { get; }


	public SearchServer(SearchClient client, int port)
	{
		Client = client;

		Handlers = new RouteCallbackMap()
		{
			["search"] = HandleRequestAsync,

		};

		var uriPrefix = $"http://*:{port}/";
		Trace.WriteLine($"{uriPrefix}");
		Listener = new SmartHttpListener(Handlers, uriPrefix);
	}


	private async Task<object> HandleRequestAsync(HttpListenerRequest request, HttpListenerResponse response)

	{
		object ok;

		var redirHdr    = request.Headers["redirect"];
		var srvResponse = new SearchServerResponse();

		try {
			var ct = request.Headers.Get("Content-Type");
			Debug.WriteLine($"{ct}");

			/*byte[] buf;

			using var memoryStream = new MemoryStream();
			await request.InputStream.CopyToAsync(memoryStream);
			buf = memoryStream.ToArray();

			memoryStream.TrySeek();
			Debug.WriteLine($"read {memoryStream.Length}");*/

			SearchQuery sq;
			object      sqInput = null;

			var mthv = MediaTypeHeaderValue.Parse(ct);
			using var sc   = new StreamContent(request.InputStream);

			var mpfd   = await MultipartFormDataParser.ParseAsync(request.InputStream);
			
			// var        parser = new StreamingMultipartFormDataParser(request.InputStream);
			// byte[]     buf1   = null;
			// FileStream fs     = new FileStream(Path.GetTempFileName(), FileMode.CreateNew);

			/*parser.FileHandler += (name, fileName, type, disposition, buffer, bytes, number, properties) =>
			{
				// buf1 = buffer;
				// buffer.CopyTo(buf1,0);
				fs.Write(buffer, 0, bytes);
			};*/

			switch (mthv.MediaType) {
				case MediaTypeNames.Text.Plain:
					goto default;

				case MediaTypeNames.Image.Bmp:
					break;

				case MediaTypeNames.Multipart.FormData:
					// sqInput = mpfd.Files[0].Data;

					// await parser.RunAsync();

					// sqInput = buf1;
					// sqInput = fs;

					var    file     = mpfd.Files.First();
					string filename = file.FileName;
					Stream data     = file.Data;
					sqInput = data;

					break;

				default:
					sqInput = await sc.ReadAsStringAsync();
					break;
			}

			/*var sz = await request.ReadRequestStringAsync();

			if (String.IsNullOrWhiteSpace(sz)) {
				return R1.Err_Query;
			}

			Debug.WriteLine($"{sz}");*/

			sq = await SearchQuery.TryCreateAsync(sqInput);

			if (sq == SearchQuery.Null) {
				srvResponse.Message = R1.Err_Query;
			}
			else {
				var url = await sq.UploadAsync();

				srvResponse.Results = await Client.RunSearchAsync(sq);
				srvResponse.Best    = SearchClient.GetBest(srvResponse.Results);
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
		Debug.WriteLine($"Disposing {nameof(SearchServer)}");
		Client?.Dispose();
		Listener?.Dispose();
		Handlers.Clear();
	}

}

public class SearchServerResponse
{

	[MN]
	[JsonPropertyOrder(0)]
	public SearchResultItem Best { get; internal set; }

	[JsonPropertyOrder(1)]
	public SearchResult[] Results { get; internal set; }

	[MN]
	[JsonPropertyOrder(2)]
	public string Message { get; internal set; }

	public SearchServerResponse() { }

	public SearchServerResponse(SearchResult[] results, SearchResultItem best)
	{
		Best    = best;
		Results = results;
	}

}
#pragma warning restore IL2026