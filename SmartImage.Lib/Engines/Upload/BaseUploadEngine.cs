using System.Diagnostics;
using System.Net;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Novus.OS;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Model;
using SmartImage.Lib.Utilities;
using SmartImage.Lib.Utilities.Diagnostics;

namespace SmartImage.Lib.Engines.Upload;

public abstract class BaseUploadEngine : IDisposable, IEndpoint
{

	/// <summary>
	/// Max file size, in bytes
	/// </summary>
	public abstract long? MaxSize { get; }

	public virtual string Name => Option.ToString();

	public Url Endpoint { get; }

	public abstract UploadEngineOptions Option { get; }

	protected BaseUploadEngine(Url s)
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

	public virtual Task<UploadResult> UploadAsync(UniImage query, CancellationToken ct = default)
	{
		Verify(query);

		if (query is UniImageUri { } uri) {
			Logger.LogTrace("Not uploading {Uni} {Val}", query, query.Value);
			var ur = new UploadResult(uri.Url, uri.Size) { };
			return Task.FromResult(ur);
		}
		else {
			return UploadFileAsync(query.Value, ct);
		}
	}

	public abstract Task<UploadResult> UploadFileAsync(string file, CancellationToken ct = default);

	protected abstract Task<UploadResult> ProcessResultAsync(IFlurlResponse response, CancellationToken ct = default);

	protected void Verify(UniImage file)
	{
		/*
		if (String.IsNullOrWhiteSpace(file)) {
			throw new ArgumentNullException(nameof(file));
		}
		*/

		if ((file.Size > MaxSize)) {
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