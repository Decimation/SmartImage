using Flurl.Http;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Engines.Search.Other;
using SmartImage.Lib.Images.Alloc;
using SmartImage.Lib.Utilities;
using SmartImage.Lib.Utilities.Diagnostics;

#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0612
namespace SmartImage.Lib.Engines.Upload.Base;

public abstract class BaseUploadEngine : IUploadEngine, IDisposable
{

	protected static readonly ILogger s_Logger = AppSupport.Factory.CreateLogger(nameof(BaseUploadEngine));

	protected static FlurlClient Client { get; }

	static BaseUploadEngine()
	{
		Client = (FlurlClient) FlurlHttp.Clients.GetOrAdd(nameof(BaseUploadEngine), null, static builder =>
		{
			builder.OnError(static f =>
			{
				//
				s_Logger.LogError(f.Exception, $"from {nameof(BaseUploadEngine)}");
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

	public abstract UploadEngineOption Option { get; }

	public TimeSpan Timeout { get; protected set; }

	protected BaseUploadEngine(Url s)
	{
		Url     = s;
		Timeout = TimeSpan.FromSeconds(15);
	}

	public abstract Task<IUploadResult> UploadFileAsync(string file, CancellationToken ct = default);

	public abstract Task<IUploadResult> ProcessResponseAsync(IFlurlResponse response, CancellationToken ct = default);

	public void Verify(IAllocImage file)
	{
		if (file.Length > MaxLength) {
			throw new ArgumentException($"File {file} is too large (max {MaxLength}) for {Name}");
		}
	}

	public static readonly UploadEngineOption[] ObsoleteUploadEngines = [UploadEngineOption.Pomf, UploadEngineOption.ImgOps];


	public static IUploadEngine GetUploadEngine(UploadEngineOption option)
	{
		if (ObsoleteUploadEngines.Contains(option)) {
			// throw new ArgumentException($"Selected option {option} is obsolete", nameof(option));
			option = SearchConfig.UE_DEFAULT;
		}

		return option switch
		{
			UploadEngineOption.Catbox    => new CatboxEngine(),
			UploadEngineOption.Litterbox => new LitterboxEngine(),
			UploadEngineOption.Pomf      => new PomfEngine(),
			UploadEngineOption.ImgOps    => new ImgOpsEngine(),
			UploadEngineOption.TmpFiles  => new TmpFilesEngine(),

			UploadEngineOption.None or _ => throw new ArgumentOutOfRangeException(nameof(option), option, null)
		};
	}

	public virtual void Dispose()
	{
		GC.SuppressFinalize(this);
	}

}