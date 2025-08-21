using System.Diagnostics;
using System.Net;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Novus.OS;
using SmartImage.Lib.Model;
using SmartImage.Lib.Utilities.Diagnostics;

namespace SmartImage.Lib.Engines.Upload;

public abstract class BaseUploadEngine : IDisposable, IEndpoint
{

	/// <summary>
	/// Max file size, in bytes
	/// </summary>
	public abstract long? MaxSize { get; }

	public virtual string Name => UploadOption.ToString();

	public Url Endpoint { get; }

	public abstract UploadEngineOptions UploadOption { get; }

	protected BaseUploadEngine(string s)
	{
		Endpoint = s;
		Timeout  = TimeSpan.FromSeconds(15);
	}

	public TimeSpan Timeout { get; protected set; }

	protected static readonly ILogger Logger = AppSupport.Factory.CreateLogger(nameof(BaseUploadEngine));

	protected static FlurlClient Client { get; }

	static BaseUploadEngine()
	{

		Client = (FlurlClient) FlurlHttp.Clients.GetOrAdd(nameof(BaseUploadEngine), null, static builder =>
		{
			builder.OnError(static f =>
			{
				//
				Logger.LogError(f.Exception, $"from {nameof(BaseUploadEngine)}");
			});
			builder.AddMiddleware(static () => new HttpLoggingHandler(Logger));

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

	public abstract Task<UploadResult> UploadFileAsync(string file, CancellationToken ct = default);

	protected virtual ValueTask<bool> Verify(UploadResult res, CancellationToken ct = default)
	{
		return ValueTask.FromResult(res.Url != null);
	}

	protected virtual Task<UploadResult> ProcessResultAsync(IFlurlResponse response,
	                                                        CancellationToken ct = default)
	{
		bool? ok = true;

		switch (response) {

			case { ResponseMessage.StatusCode: HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout }:
			case null:
				ok = false;

				goto ret;

		}

	ret:

		var result = new UploadResult
		{
			Size    = response.Headers.TryGetFirst("Content-Length", out var cls) ? Int64.Parse(cls) : null,
			IsValid = ok
		};


		return Task.FromResult(result);
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