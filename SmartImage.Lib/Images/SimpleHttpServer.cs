// Author: Deci | Project: SmartImage.Lib | Name: SimpleHttpServer.cs
// Date: 2024/11/21 @ 03:11:34

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using Flurl.Http;

namespace SmartImage.Lib.Images;

using Funcs = Dictionary<string, Func<byte[], string>>;

// From https://github.com/chrishonselaar/ProtoPad/blob/master/ServiceDiscovery/SimpleHttpServer.cs

public sealed class SimpleHttpServer : IDisposable
{

	public HttpListener Listener { get; }

	private const int ChunkSize = 1024;

	private readonly Funcs m_requestHandlers;

	public static FlurlClient Client { get; }

	private sealed class HttpResponseState
	{

		public Stream Stream { get; init; }

		public byte[] Buffer { get; init; }

		public readonly ConcurrentBag<byte[]> Result = new();

		// public readonly ArrayPool<byte> Result = ArrayPool<byte>.Create();

		public HttpListenerRequest  Request;
		public HttpListenerResponse Response;

	}


	public SimpleHttpServer(int port, Funcs requestHandlers)
	{
		m_requestHandlers = requestHandlers;
		Listener          = new HttpListener();
		Listener.Prefixes.Add($"http://*:{port}/");
		Start();

		Debug.WriteLine("ProtoPad HTTP Server started");
	}

	private void Start()
	{
		if (!Listener.IsListening) {
			Listener.Start();
			Listener.BeginGetContext(HandleRequest, Listener);

		}
	}

	private void Callback(IAsyncResult ar)
	{
		var state = (HttpResponseState) ar.AsyncState;

		if (state == null) {
			return;
		}

		var bytesRead = state.Stream.EndRead(ar);

		if (bytesRead > 0) {
			var buffer = new byte[bytesRead];
			Buffer.BlockCopy(state.Buffer, 0, buffer, 0, bytesRead);
			state.Result.Add(buffer);
			state.Stream.BeginRead(state.Buffer, 0, state.Buffer.Length, Callback, state);
		}
		else {
			state.Stream.Dispose();
			var responseData = state.Result.SelectMany(static x => x).ToArray();

			// var responseData = state.Result;

			foreach (var requestHandler in m_requestHandlers) {


				var requestUrl = state.Request.Url;

				if (requestUrl != null && !requestUrl.PathAndQuery.Contains(requestHandler.Key))
					continue;

				var responseValue = requestHandler.Value(responseData);
				var responseBytes = Encoding.UTF8.GetBytes(responseValue);

				state.Response.ContentType     = MediaTypeNames.Text.Plain;
				state.Response.StatusCode      = (int) HttpStatusCode.OK;
				state.Response.ContentLength64 = responseBytes.Length;
				state.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
				state.Response.OutputStream.Close();
				return;
			}
		}
	}

	private void HandleRequest(IAsyncResult result)
	{
		var context = Listener.EndGetContext(result);
		Listener.BeginGetContext(HandleRequest, Listener);

		var state = new HttpResponseState
		{
			Stream   = context.Request.InputStream,
			Buffer   = new byte[ChunkSize],
			Response = context.Response,
			Request  = context.Request
		};

		context.Request.InputStream.BeginRead(state.Buffer, 0, state.Buffer.Length, Callback, state);
	}


	public static async Task<IFlurlResponse> SendCustomCommandAsync(string ipAddress, string command,
	                                                                CancellationToken ct = default)
	{
		try {
			string s = $"{ipAddress}/{command}";

			var res = await Client.Request(s)
				          .GetAsync(cancellationToken: ct);
			return res;
		}
		catch {
			return null;
		}
	}

	public static async Task<IFlurlResponse> SendPostRequestAsync(string ipAddress, byte[] byteArray, string command)
	{
		var request = Client.Request($"{ipAddress}/{command}");
		request.Content.Headers.ContentLength = byteArray.Length;
		request.Content.Headers.ContentType   = MediaTypeHeaderValue.Parse(MediaTypeNames.Application.FormUrlEncoded);
		request.Content                       = new ByteArrayContent(byteArray);

		try {
			var response = await request.SendAsync(HttpMethod.Post);

			// var stream   = await response.GetStreamAsync();

			return response;
		}
		catch (Exception) {
			return null;
		}
	}

	public void Dispose()
	{

		Listener?.Close();
	}

}