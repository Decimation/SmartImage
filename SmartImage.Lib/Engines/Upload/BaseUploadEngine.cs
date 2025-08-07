using System.Diagnostics;
using System.Net;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Novus.OS;
using SmartImage.Lib.Utilities.Diagnostics;

namespace SmartImage.Lib.Engines.Upload;

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

	protected virtual async ValueTask<bool> Verify(UploadResult res,CancellationToken ct=default)
	{
		return res.Url != null;
	}

	protected virtual async Task<UploadResult> ProcessResultAsync(IFlurlResponse response,
	                                                              CancellationToken ct = default)
	{
		Url url = null;
		bool?   ok = null;

		ok  = true;

	ret:
		switch (response) {

			case { ResponseMessage.StatusCode: HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout }:
			case null:
				ok  = false;
				url = null;

				goto ret;

		}

		var result = new UploadResult
		{
			// Url      = url,
			Size     = response.Headers.TryGetFirst("Content-Length", out var cls) ? Int64.Parse(cls) : null,
			IsValid  = ok
		};



		return result;
	}

	protected void Verify(string file)
	{
		if (String.IsNullOrWhiteSpace(file)) {
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