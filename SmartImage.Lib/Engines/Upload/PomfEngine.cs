using System.Diagnostics;
using System.Text.Json;
using Flurl.Http;
using SmartImage.Lib.Engines.Upload.Base;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Engines.Upload;

[Obsolete]
public sealed class PomfEngine : BaseUploadEngine
{

	public override UploadEngineOption Option => UploadEngineOption.Pomf;

	public PomfEngine() : base("https://pomf.lain.la/upload.php") { }

	public override long? MaxLength => 1_000_000_000;

	public override async Task<IUploadResult> UploadFileAsync(string file, CancellationToken ct = default)
	{
		using var response = await Client.Request(Url).WithTimeout(Timeout).PostMultipartAsync(mp =>
		{
			//
			mp.AddFile("files[]", file);
		}, cancellationToken: ct);

		if (response == null) {
			Debugger.Break();

			return null;
		}

		var ur = await ProcessResponseAsync(response, ct);

		return ur;
	}

	public override async Task<IUploadResult> ProcessResponseAsync(IFlurlResponse response, CancellationToken ct = default)
	{
		// var pr = await response.GetJsonAsync<PomfResult>();
		var sz = await response.GetStringAsync();

		var pr    = JsonSerializer.Deserialize<PomfResult>(sz, HttpUtil.DefaultSerializerOptions);
		var file0 = pr.Files.First();

		return new UploadResult(file0.Url, file0.Length);
	}

}

public sealed class PomfResult
{

	public bool Success { get; set; }

	public PomfFileResult[] Files { get; set; }

}

public sealed class PomfFileResult : UploadResult
{

	public string Hash { get; set; }

	public string Name { get; set; }

}