using Flurl.Http;
using Kantan.Net.Utilities;
using System.Net;

namespace SmartImage.Lib.Engines.Upload;

public abstract class BaseCatboxEngine : BaseUploadEngine
{

	public override UploadEngineOptions UploadOption => UploadEngineOptions.Catbox;

	public override async Task<UploadResult> UploadFileAsync(string file, CancellationToken ct = default)
	{

		var response = await Client.Request(Endpoint)
			               .WithSettings(r => { r.Timeout = Timeout; })
			               .WithHeaders(new
			               {
				               User_Agent = HttpUtilities.UserAgent
			               })
			               .PostMultipartAsync(mp =>
			               {
				               mp.AddFile("fileToUpload", file)
					               .AddString("reqtype", "fileupload")
					               .AddString("time", "1h")
					               .AddString("userhash", String.Empty);
			               }, cancellationToken: ct, completionOption: HttpCompletionOption.ResponseHeadersRead);

		return await ProcessResultAsync(response, ct).ConfigureAwait(false);
	}

	protected override async Task<UploadResult> ProcessResultAsync(IFlurlResponse response, CancellationToken ct = default)
	{
		var ur = await base.ProcessResultAsync(response, ct);

		ur.Url = await response.ResponseMessage.Content.ReadAsStringAsync(ct);
		return ur;
	}

	/*public async Task<UploadResult> UploadFileAsync(Stream file, CancellationToken ct = default)
	{

		var response = await Client.Request(EndpointUrl)
			               .WithSettings(r => { r.Timeout = Timeout; })
			               .WithHeaders(new
			               {
				               User_Agent = HttpUtilities.UserAgent
			               })
			               .PostMultipartAsync(mp =>
			               {
				               mp.AddFile("fileToUpload", file, Path.GetTempFileName())
					               .AddString("reqtype", "fileupload")
					               .AddString("time", "1h")
					               .AddString("userhash", string.Empty);
			               }, cancellationToken: ct, completionOption: HttpCompletionOption.ResponseHeadersRead);

		return await ProcessResultAsync(response, ct).ConfigureAwait(false);
	}*/

	protected BaseCatboxEngine(string s) : base(s) { }

}

public sealed class CatboxEngine : BaseCatboxEngine
{

	public override long? MaxSize => 200_000_000L;

	public CatboxEngine() : base("https://catbox.moe/user/api.php") { }

}