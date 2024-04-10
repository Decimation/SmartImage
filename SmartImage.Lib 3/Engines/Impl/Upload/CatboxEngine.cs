using System.ComponentModel;
using Flurl.Http;
using Kantan.Net.Utilities;
using SmartImage.Lib.Results;

namespace SmartImage.Lib.Engines.Impl.Upload;

public abstract class BaseCatboxEngine : BaseUploadEngine
{

	public override async Task<UploadResult> UploadFileAsync(string file, CancellationToken ct = default)
	{
		Verify(file);

		var response = await Client.Request(EndpointUrl)
			               .WithSettings(r =>
			               {
				               r.Timeout = Timeout;
			               })
			               .WithHeaders(new
			               {
				               User_Agent = HttpUtilities.UserAgent
			               })
			               .PostMultipartAsync(mp =>
			               {
				               mp.AddFile("fileToUpload", file)
					               .AddString("reqtype", "fileupload")
					               .AddString("time", "1h")
					               .AddString("userhash", string.Empty);
			               }, cancellationToken: ct, completionOption: HttpCompletionOption.ResponseHeadersRead);

		return await ProcessResultAsync(response, ct).ConfigureAwait(false);
	}

	protected BaseCatboxEngine(string s) : base(s) { }

}

public sealed class CatboxEngine : BaseCatboxEngine
{

	public override string Name => "Catbox";

	public override long? MaxSize => 200_000_000L;

	public static readonly BaseCatboxEngine Instance = new CatboxEngine();

	public CatboxEngine() : base("https://catbox.moe/user/api.php")
	{
		EnsureResponse = true;
	}

}