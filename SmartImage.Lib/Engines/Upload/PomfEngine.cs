using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Flurl.Http;
using Kantan.Net.Utilities;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Engines.Upload;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed class PomfEngine : BaseUploadEngine
{

	public override UploadEngineOptions Option => UploadEngineOptions.Pomf;

	public PomfEngine() : base("https://pomf.lain.la/upload.php") { }

	public override long? MaxLength => 1_000_000_000;

	public override async Task<UploadResult> UploadFileAsync(string file, CancellationToken ct = default)
	{
		using var response = await Client.Request(Endpoint)
			                     .WithTimeout(Timeout)
			                     .OnError(r =>
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
		// var pr = await response.GetJsonAsync<PomfResult>();
		var sz = await response.GetStringAsync();

		var pr   = JsonSerializer.Deserialize<PomfResult>(sz, SearchUtil.DefaultSerializerOptions);
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