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
using Kantan.Net.Utilities;

namespace SmartImage.Lib.Images;

using Funcs = Dictionary<string, Func<byte[], string>>;
using Funcs2 = Dictionary<string, SimpleHttpServer.RequestDataCallback>;
using Funcs3 = Dictionary<string, SimpleHttpServer.RequestData2Callback>;

// From https://github.com/chrishonselaar/ProtoPad/blob/master/ServiceDiscovery/SimpleHttpServer.cs

public sealed class SimpleHttpServer : IDisposable
{

	static SimpleHttpServer()
	{
		Client = (FlurlClient) FlurlHttp.Clients.GetOrAdd(nameof(SimpleHttpServer), null, builder =>
		{
			// builder.Settings.Redirects.ForwardAuthorizationHeader = true;
			// builder.Settings.Redirects.AllowSecureToInsecure      = true;

			builder.Settings.AllowedHttpStatusRange = "*";
			builder.AllowAnyHttpStatus();

			builder.OnError(f =>
			{
				f.ExceptionHandled = true;
				return;
			});

		});
	}

	public HttpListener Listener { get; }

	private const int ChunkSize = 1024;

	public Funcs3 RequestHandlers { get; }

	public static FlurlClient Client { get; }


	public delegate Task<string> RequestDataCallback(byte[] buf);

	public delegate Task<HttpListenerResponse> RequestDataCallback2(HttpListenerRequest buf);

	public SimpleHttpServer(int port, Funcs3 requestHandlers)
	{
		RequestHandlers = requestHandlers;
		Listener        = new HttpListener();
		Listener.Prefixes.Add($"http://*:{port}/");

		// Start();

		// Debug.WriteLine("ProtoPad HTTP Server started");
	}


	public async Task StartAsync(CancellationToken ct = default)
	{
		if (!Listener.IsListening) {
			Listener.Start();

			// Listener.BeginGetContext(HandleRequest, Listener);
			while (Listener.IsListening) {
				var ctx = await Listener.GetContextAsync();
				var res = await HandleRequestAsync(ctx, ct);

				if (ct.IsCancellationRequested) {
					break;
				}
			}
		}
	}

	private async Task<bool> HandleRequest2Async(HttpListenerContext context, CancellationToken ct = default)
	{

		// var bytesRead = state.Stream.Length;

		var responseData = new byte[context.Request.ContentLength64];
		var cb           = await context.Request.InputStream.ReadAsync(responseData, 0, responseData.Length, ct);


		// var responseData = state.ResultBuffer;

		foreach (var requestHandler in RequestHandlers) {


			var requestUrl = context.Request.Url;

			if (requestUrl != null && !requestUrl.PathAndQuery.Contains(requestHandler.Key))
				continue;

			var responseValue = await requestHandler.Value(responseData);

			context.Response.Close();

			return true;
		}

		return true;
	}

	private async Task<bool> HandleRequestAsync(HttpListenerContext context, CancellationToken ct = default)
	{

		// var bytesRead = state.Stream.Length;

		var responseData = new byte[context.Request.ContentLength64];
		var cb           = await context.Request.InputStream.ReadAsync(responseData, 0, responseData.Length, ct);


		// var responseData = state.ResultBuffer;

		foreach (var requestHandler in RequestHandlers) {


			var requestUrl = context.Request.Url;

			if (requestUrl != null && !requestUrl.PathAndQuery.Contains(requestHandler.Key))
				continue;

			var responseValue = await requestHandler.Value(responseData);
			var responseBytes = Encoding.UTF8.GetBytes(responseValue);

			context.Response.ContentType     = MediaTypeNames.Text.Plain;
			context.Response.StatusCode      = (int) HttpStatusCode.OK;
			context.Response.ContentLength64 = responseBytes.Length;
			await context.Response.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length, ct);

			// context.Response.OutputStream.Close();

			// state.Dispose();
			context.Response.Close();

			return true;
		}

		return true;
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

#if ASYNC_OLD
	private sealed class HttpResponseState : IDisposable
	{

		public HttpResponseState(HttpListenerRequest request, HttpListenerResponse response)
		{
			Request = request;
			Response = response;
			Stream = Request.InputStream;
			Buffer = new byte[ChunkSize];
			ResultBuffer = new ConcurrentBag<byte[]>();

			Trace.WriteLine(
				$"Alloc {nameof(HttpResponseState)} :: {Request.ContentLength64} {Response.ContentLength64}");
		}

		public Stream Stream { get; }

		public byte[] Buffer { get; }

		public ConcurrentBag<byte[]> ResultBuffer { get; }

		// public readonly ArrayPool<byte> ResultBuffer = ArrayPool<byte>.Create();

		public HttpListenerRequest Request { get; }

		public HttpListenerResponse Response { get; }

		public void Dispose()
		{
			Stream?.Dispose();
			((IDisposable) Response)?.Dispose();
			ResultBuffer.Clear();
		}

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
			state.ResultBuffer.Add(buffer);
			state.Stream.BeginRead(state.Buffer, 0, state.Buffer.Length, Callback, state);
		}
		else {
			state.Stream.Dispose();
			var responseData = state.ResultBuffer.SelectMany(static x => x).ToArray();

			// var responseData = state.ResultBuffer;

			foreach (var requestHandler in m_requestHandlers) {


				var requestUrl = state.Request.Url;

				if (requestUrl != null && !requestUrl.PathAndQuery.Contains(requestHandler.Key))
					continue;

				var responseValue = requestHandler.Value(responseData);
				var responseBytes = Encoding.UTF8.GetBytes(responseValue);

				state.Response.ContentType = MediaTypeNames.Text.Plain;
				state.Response.StatusCode = (int) HttpStatusCode.OK;
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

		var state = new HttpResponseState(context.Request, context.Response)
			{ };

		context.Request.InputStream.BeginRead(state.Buffer, 0, state.Buffer.Length, Callback, state);
	}
#endif

}