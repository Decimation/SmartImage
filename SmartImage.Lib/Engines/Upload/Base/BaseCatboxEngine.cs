// Author: Deci | Project: SmartImage.Lib | Name: BaseCatboxEngine.cs
// Date: 2026/02/21 @ 15:02:55

using Flurl.Http;
using Flurl.Http.Content;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Engines.Upload.Base;

[Obsolete("No longer in service")]
public abstract class BaseCatboxEngine : BaseUploadEngine
{

	public override UploadEngineOptions Option => UploadEngineOptions.Catbox;

	protected BaseCatboxEngine(string s) : base(s) { }

	protected virtual IFlurlRequest BuildRequest()
	{
		return Client.Request(Url)
		             .WithTimeout(Timeout)
		             .WithHeaders(new { User_Agent = R1.UserAgent1 });
	}

	protected virtual CapturedMultipartContent BuildContent(CapturedMultipartContent mp, string file)
	{
		return mp.AddFile("fileToUpload", file)
		         .AddString("reqtype", "fileupload")
		         .AddString("time", "1h")
		         .AddString("userhash", String.Empty);
	}

	public override async Task<UploadResult> UploadFileAsync(string file, CancellationToken ct = default)
	{
		using var response = await BuildRequest().PostMultipartAsync(mp =>
		{
			//
			mp = BuildContent(mp, file);
		}, cancellationToken: ct);

		var ur = await ProcessResponseAsync(response, ct);

		return ur;
	}

	public override async Task<UploadResult> ProcessResponseAsync(IFlurlResponse response, CancellationToken ct = default)
	{
		var   url  = await response.ResponseMessage.Content.ReadAsStringAsync(ct);
		long? size = response.TryGetContentLength();

		return new UploadResult(url, size);
	}

}