using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Flurl.Http;
using Microsoft.Identity.Client;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Engines.Upload;

public class TmpFilesEngine : BaseUploadEngine
{

	public const string TMPFILES_URL_BASE = "https://tmpfiles.org";
	public const string TMPFILES_URL_API  = $"{TMPFILES_URL_BASE}/api/v1/upload";

	public TmpFilesEngine() : base(TMPFILES_URL_BASE) { }

	public override long? MaxLength => 100_000_000;

	public override UploadEngineOptions Option => UploadEngineOptions.TmpFiles;

	public override async Task<UploadResult> UploadFileAsync(string file, CancellationToken ct = default)
	{
		using var req = await Client.Request(TMPFILES_URL_API).PostMultipartAsync(act =>
		{
			//
			act.AddFile("file", file);
		}, cancellationToken: ct);

		var prc = await ProcessResponseAsync(req, ct);
		return prc;
	}

	public override async Task<UploadResult> ProcessResponseAsync(IFlurlResponse response, CancellationToken ct = default)
	{
		var str    = await response.GetStringAsync();
		var tmpRes = JsonSerializer.Deserialize<TmpFilesResponse>(str, SearchUtil.DefaultSerializerOptions);
		var ur     = new UploadResult(tmpRes.Data.Url,default);
		return ur;
	}

}

#region 

public class TmpFilesResponse
{
	public string Status { get; set; }

	public TmpFilesResponseData Data { get; set; }
}

public class TmpFilesResponseData
{
	public Url Url { get; set; }
}

#endregion