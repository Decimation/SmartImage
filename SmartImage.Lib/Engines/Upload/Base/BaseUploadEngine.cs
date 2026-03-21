using Flurl.Http;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Engines.Search.Other;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Utilities;
using SmartImage.Lib.Utilities.Diagnostics;

#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0612
namespace SmartImage.Lib.Engines.Upload.Base;

public abstract class BaseUploadEngine : IUploadEngine, IDisposable
{

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

			// builder.AddMiddleware(static () => new HttpLoggingHandler(Logger));

		});
	}

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

	public abstract Task<IUploadResult> UploadFileAsync(string file, CancellationToken ct = default);

	public abstract Task<IUploadResult> ProcessResponseAsync(IFlurlResponse response, CancellationToken ct = default);

	public void Verify(IUniImage file)
	{
		if (file.Length > MaxLength) {
			throw new ArgumentException($"File {file} is too large (max {MaxLength}) for {Name}");
		}
	}


	public virtual void Dispose()
	{
		GC.SuppressFinalize(this);
	}

	public static IUploadEngine GetUploadEngine(UploadEngineOptions options)
	{
		return options switch
		{
			UploadEngineOptions.Catbox    => new CatboxEngine(),
			UploadEngineOptions.Litterbox => new LitterboxEngine(),
			UploadEngineOptions.Pomf      => new PomfEngine(),
			UploadEngineOptions.ImgOps    => new ImgOpsEngine(),
			UploadEngineOptions.TmpFiles  => new TmpFilesEngine(),

			UploadEngineOptions.None or _ => throw new ArgumentOutOfRangeException(nameof(options), options, null)
		};
	}

}