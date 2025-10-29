using Flurl.Http;
using Kantan.Net.Utilities;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Net.Http.Headers;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Utilities;
using SmartImage.Lib.Engines.Results;

namespace SmartImage.Lib.Engines.Upload;

public abstract class BaseCatboxEngine : BaseUploadEngine
{

	public override UploadEngineOptions Option => UploadEngineOptions.Catbox;


	public override async Task<UploadResult> UploadFileAsync(string file, CancellationToken ct = default)
	{
		using var response = await Client.Request(Endpoint)
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

		var ur = await ProcessResultAsync(response, ct);

		return ur;
	}

	protected override async Task<UploadResult> ProcessResultAsync(IFlurlResponse response, CancellationToken ct = default)
	{

		var url = await response.ResponseMessage.Content.ReadAsStringAsync(ct);
		long? size = response.TryGetContentLength();
		
		return new UploadResult(url,size);
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