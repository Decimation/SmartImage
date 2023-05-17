using System.Diagnostics;
using System.Net.NetworkInformation;
using Flurl.Http;
using Novus.OS;
using Novus.Utilities;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Engines.Impl.Upload;

public abstract class BaseUploadEngine : IEndpoint
{
	/// <summary>
	/// Max file size, in bytes
	/// </summary>
	public abstract int MaxSize { get; }

	public abstract string Name { get; }

	public Url EndpointUrl { get; }

	protected BaseUploadEngine(string s)
	{
		EndpointUrl = s;
	}

	// public static BaseUploadEngine Default { get; } = new LitterboxEngine();
	
	public abstract Task<Url> UploadFileAsync(string file, CancellationToken ct = default);

	public long Size { get; set; }

	private protected bool IsFileSizeValid(string file)
	{
		Size = FileSystem.GetFileSize(file);
		var b = Size > MaxSize;

		return !b;
	}

	protected void Verify(string file)
	{
		if (string.IsNullOrWhiteSpace(file)) {
			throw new ArgumentNullException(nameof(file));
		}

		if (!IsFileSizeValid(file)) {
			throw new ArgumentException($"File {file} is too large (max {MaxSize}) for {Name}");
		}
	}

	public static readonly BaseUploadEngine[] All =
		ReflectionHelper.CreateAllInAssembly<BaseUploadEngine>(TypeProperties.Subclass).ToArray();

	public static BaseUploadEngine Default { get; set; } = new CatboxEngine();
}

public abstract class BaseCatboxEngine : BaseUploadEngine
{
	public override async Task<Url> UploadFileAsync(string file, CancellationToken ct = default)
	{
		Verify(file);

		using var response = await EndpointUrl
			                     .PostMultipartAsync(mp =>
				                                         mp.AddFile("fileToUpload", file)
					                                         .AddString("reqtype", "fileupload")
					                                         .AddString("time", "1h")
			                     , ct);

		var responseMessage = response.ResponseMessage;

		var content = await responseMessage.Content.ReadAsStringAsync(ct);

		/*if (!responseMessage.IsSuccessStatusCode) {

		return null;
	}*/

		return new(content);
	}

	protected BaseCatboxEngine(string s) : base(s) { }
}