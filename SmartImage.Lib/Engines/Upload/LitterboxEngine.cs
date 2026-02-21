// ReSharper disable StringLiteralTypo

// ReSharper disable UnusedMember.Global

using Flurl.Http;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Images.Uni;
using System.Diagnostics;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Engines.Upload;

public sealed class LitterboxEngine : BaseCatboxEngine
{

	private const string LITTERBOX_BASE_URL = "https://litterbox.catbox.moe";

	public override UploadEngineOptions Option => UploadEngineOptions.Litterbox;

	public override long? MaxLength => 1_000_000_000L;

	public LitterboxEngine() : base("https://litterbox.catbox.moe/resources/internals/api.php") { }

	public override async Task<UploadResult> UploadFileAsync(string file, CancellationToken ct = default)
	{
		using var response = await Client.Request(Url)
			                     .WithSettings(r => { r.Timeout = Timeout; })
			                     .PostMultipartAsync(mp =>
			                     {
				                     mp.RemoveQuotesFromContentTypeBoundary();

				                     mp.AddFile("fileToUpload", file)
					                     .AddString("time", "1h")
					                     .AddString("reqtype", "fileupload");
			                     }, cancellationToken: ct);

		var ur = await ProcessResultAsync(response, ct);

		return ur;
	}

}