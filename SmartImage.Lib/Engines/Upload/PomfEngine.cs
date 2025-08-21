using System.Diagnostics;
using System.Text.Json.Nodes;
using Flurl.Http;

namespace SmartImage.Lib.Engines.Upload;

public sealed class PomfEngine : BaseUploadEngine
{

	public override UploadEngineOptions UploadOption => UploadEngineOptions.Pomf;

	public PomfEngine() : base("https://pomf.lain.la/upload.php") { }

	public override long? MaxSize => 1_000_000_000;

	[ICBN]
	public override async Task<UploadResult> UploadFileAsync(string file, CancellationToken ct = default)
	{
		Verify(file);

		using var response = await Client.Request(Endpoint)
			                     .WithSettings(r => { r.Timeout = Timeout; }).OnError(r =>
			                     {
				                     r.ExceptionHandled = true;
				                     Trace.WriteLine($"{r.Exception.Message}: {file} {Name}");
			                     })
			                     .PostMultipartAsync(mp =>
			                     {
				                     //
				                     mp.AddFile("files[]", file);
			                     }, cancellationToken: ct);

		if (response == null) {
			Debugger.Break();

			return new UploadResult()
			{
				IsValid = false
			};
		}

		var pr = await response.GetJsonAsync<PomfResult>();

		var bur = new PomfResult()
		{
			Size    = pr.Files.Sum(x => x.Size),
			Url     = pr.Files[0].Url,
			IsValid = pr.Success
		};

		return bur;
	}

}

public sealed class PomfResult : UploadResult
{

	public bool Success { get; set; }

	public PomfFileResult[] Files { get; set; }

}

public sealed class PomfFileResult
{

	public string Hash { get; set; }

	public string Name { get; set; }

	public string Url { get; set; }

	public long Size { get; set; }

}