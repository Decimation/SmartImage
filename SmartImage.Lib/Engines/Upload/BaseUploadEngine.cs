using System.Diagnostics;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Engines.Search.Other;
using SmartImage.Lib.Images;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Model;
using SmartImage.Lib.Utilities;
using SmartImage.Lib.Utilities.Diagnostics;

namespace SmartImage.Lib.Engines.Upload;

public abstract class BaseUploadEngine : IUploadEngine, IDisposable
{
	public Url Url { get; }

	public string Name => Option.ToString();

	/// <summary>
	/// Max file size, in bytes
	/// </summary>
	public abstract long? MaxLength { get; }

	public abstract UploadEngineOptions Option { get; }

	public TimeSpan Timeout { get; protected set; }

	protected BaseUploadEngine(Url s)
	{
		Url     = s;
		Timeout = TimeSpan.FromSeconds(15);
	}

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

	public static IUploadEngine GetUploadEngine(UploadEngineOptions options)
	{
		return options switch
		{
			UploadEngineOptions.Catbox    => new CatboxEngine(),
			UploadEngineOptions.Litterbox => new LitterboxEngine(),
			UploadEngineOptions.Pomf      => new PomfEngine(),
			UploadEngineOptions.ImgOps      => new ImgOpsEngine(),
			UploadEngineOptions.None or _ => throw new ArgumentOutOfRangeException(nameof(options), options, null)
		};
	}

	//todo

	public virtual Task<UploadResult> UploadAsync(UniImage query, CancellationToken ct = default)
	{
		Verify(query);

		if (query is UniImageUrl { } uri) {
			Logger.LogTrace("Not uploading {Uni} {Val}", query, query.Value);
			var ur = new UploadResult(uri.Url, uri.Length) { };
			return Task.FromResult(ur);
		}

		return UploadFileAsync(query.Value, ct);
	}

	public abstract Task<UploadResult> UploadFileAsync(string file, CancellationToken ct = default);

	public abstract Task<UploadResult> ProcessResponseAsync(IFlurlResponse response, CancellationToken ct = default);

	public void Verify(UniImage file)
	{
		if (file.Length > MaxLength) {
			throw new ArgumentException($"File {file} is too large (max {MaxLength}) for {Name}");
		}
	}


	public virtual void Dispose()
	{
		GC.SuppressFinalize(this);
	}

}