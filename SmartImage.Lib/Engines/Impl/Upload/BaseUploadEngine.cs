using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Content;
using Kantan.Net.Utilities;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;
using Novus.OS;
using Novus.Utilities;
using SmartImage.Lib.Results;
using SmartImage.Lib.Utilities;
using SmartImage.Lib.Utilities.Diagnostics;

namespace SmartImage.Lib.Engines.Impl.Upload;

public abstract class BaseUploadEngine : IDisposable
{

	/// <summary>
	/// Max file size, in bytes
	/// </summary>
	public abstract long? MaxSize { get; }

	public virtual string Name => UploadOption.ToString();

	public string EndpointUrl { get; }

	public abstract UploadEngineOptions UploadOption { get; }

	protected BaseUploadEngine(string s)
	{
		EndpointUrl = s;
		Timeout     = TimeSpan.FromSeconds(15);
	}

	// public static BaseUploadEngine Default { get; } = new LitterboxEngine();

	public TimeSpan Timeout { get; protected set; }

	protected static readonly ILogger Logger = AppSupport.Factory.CreateLogger(nameof(BaseUploadEngine));

	protected static FlurlClient Client { get; }

	static BaseUploadEngine()
	{
		/*var handler = new LoggingHttpMessageHandler(Logger)
		{
			InnerHandler = new HttpLoggingHandler(Logger)
			{
				InnerHandler = new HttpClientHandler()
			}
		};

		Client = new FlurlClient(new HttpClient(handler))
		{
			Settings =
			{
				Redirects =
				{
					Enabled                    = true,
					AllowSecureToInsecure      = true,
					ForwardAuthorizationHeader = true,
					MaxAutoRedirects           = 20,
				},
			},
		};*/

		Client = (FlurlClient) FlurlHttp.Clients.GetOrAdd(nameof(BaseUploadEngine), null, builder =>
		{
			builder.OnError(f =>
			{
				//
				Logger.LogError(f.Exception, $"from {nameof(BaseUploadEngine)}");
			});
			builder.AddMiddleware(() => new HttpLoggingHandler(Logger));

		});
	}

	public static BaseUploadEngine GetUploadEngine(UploadEngineOptions options)
	{
		return options switch
		{
			UploadEngineOptions.Catbox    => new CatboxEngine(),
			UploadEngineOptions.Litterbox => new LitterboxEngine(),
			UploadEngineOptions.Pomf      => new PomfEngine(),
			UploadEngineOptions.None or _ => throw new ArgumentOutOfRangeException(nameof(options), options, null)
		};
	}

	//todo
	private static BaseUploadEngine _default = GetUploadEngine(SearchConfig.UPLOAD_ENGINE_DEFAULT);

	public static BaseUploadEngine Default
	{
		get { return _default; }
		set
		{
			_default?.Dispose();
			_default = value;
		}
	}

	/*
	public static async Task<UploadResult> UploadAutoAsync(BaseUploadEngine engine, string fu,
	                                                       CancellationToken ct = default)
	{
		// TODO

		// engine ??= BaseUploadEngine.Default;
		UploadResult u;
		int          i = 0;

		bool ok;

		do {
			u  = await engine.UploadFileAsync(fu, ct);
			ok = u.IsValid;

			if (!ok) {
				Debug.WriteLine($"{u} is invalid!");

				// Debugger.Break();
				if (i++ < BaseUploadEngine.All.Length) {
					engine = BaseUploadEngine.All[i];
					Debug.WriteLine($"Trying {engine.Name}");
					ok = true;
				}
				else {
					ok = false;
				}
			}
			else {
				return u;
			}

		} while (!ok);

		return u;
	}
	*/

	public abstract Task<UploadResult> UploadFileAsync(string file, CancellationToken ct = default);

	protected virtual async Task<UploadResult> ProcessResultAsync(IFlurlResponse response,
	                                                              CancellationToken ct = default)
	{
		string url = null;
		bool   ok;


		switch (response) {

			case { ResponseMessage.StatusCode: HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout }:
			case null:
				ok  = false;
				url = null;

				goto ret;

		}

		/*if (response == null) {
			ok = false;

			goto ret;
		}

		var responseMessage = response.ResponseMessage;

		switch (responseMessage.StatusCode) {
			case HttpStatusCode.BadGateway:
			case HttpStatusCode.GatewayTimeout:
				url = null;
				ok  = false;
				goto ret;
		}*/

		url = await response.ResponseMessage.Content.ReadAsStringAsync(ct);
		ok  = true;

		/*if (ensureResponse) {
			var fmt = await ISImage.DetectFormatAsync(await response.GetStreamAsync(), ct);

			/*var r2 = await Client.Request(url)
				         .WithSettings(r =>
				         {
					         r.Timeout = Timeout;
				         }).OnError(rx =>
				         {
					         // Debugger.Break();
					         rx.ExceptionHandled = true;
				         }).GetAsync(cancellationToken: ct);

			if (r2 == null || r2.GetContentLength() == 0) {
				ok = false;
			}#1#

		}*/

	ret:

		// var fmt = await ISImage.DetectFormatAsync(await response.GetStreamAsync(), ct);

		return new UploadResult
		{
			Url      = url,
			Size     = response.Headers.TryGetFirst("Content-Length", out var cls) ? long.Parse(cls) : null,
			IsValid  = ok,
			Response = response
		};
	}

	protected void Verify(string file)
	{
		if (string.IsNullOrWhiteSpace(file)) {
			throw new ArgumentNullException(nameof(file));
		}

		if ((FileSystem.GetFileSize(file) > MaxSize)) {
			throw new ArgumentException($"File {file} is too large (max {MaxSize}) for {Name}");
		}
	}

	/*
	public static readonly BaseUploadEngine[] All =
		ReflectionHelper.CreateAllInAssembly<BaseUploadEngine>(InheritanceProperties.Subclass).ToArray();
		*/


	public void Dispose()
	{
		Debug.WriteLine($"Disposing {nameof(BaseUploadEngine)} ({Name})");
		GC.SuppressFinalize(this);
	}

}