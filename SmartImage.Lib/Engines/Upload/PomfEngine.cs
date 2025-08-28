using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Flurl.Http;
using SmartImage.Lib.Images.Uni;

namespace SmartImage.Lib.Engines.Upload;

public sealed class PomfEngine : BaseUploadEngine
{

	public override UploadEngineOptions Option => UploadEngineOptions.Pomf;

	public PomfEngine() : base("https://pomf.lain.la/upload.php") { }

	public override long? MaxSize => 1_000_000_000;

	public override async Task<UploadResult> UploadFileAsync(string file, CancellationToken ct = default)
	{
		using var response = await Client.Request(Endpoint)
			                     .WithSettings(r =>
			                     {
				                     //...
				                     r.Timeout = Timeout;
			                     }).OnError(r =>
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

			return null;
		}

		var ur = await ProcessResultAsync(response, ct);

		return ur;
	}

	protected override async Task<UploadResult> ProcessResultAsync(IFlurlResponse response, CancellationToken ct = default)
	{
		var pr = await response.GetJsonAsync<PomfResult>();

		var size = pr.Files.Sum(x => x.Size);
		var url  = pr.Files[0].Url;

		var bur = new PomfResult(url, size)
			{ };

		return bur;
	}

}

public sealed class PomfResult : UploadResult
{

	public bool Success { get; set; }

	public PomfFileResult[] Files { get; set; }

	[JsonConstructor]
	public PomfResult(Url url, long? size, bool success, PomfFileResult[] files) : base(url, size)
	{
		Success = success;
		Files   = files;
	}

	// [JsonConstructor]
	internal PomfResult(Url url, long? size) : base(url, size) { }

}

public sealed class PomfFileResult
{

	public string Hash { get; set; }

	public string Name { get; set; }

	public string Url { get; set; }

	public long Size { get; set; }

}