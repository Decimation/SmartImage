using Flurl.Http;
using Flurl.Http.Content;
using Microsoft.Identity.Client;
using SmartImage.Lib.Engines.Upload;
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Model;
using System.Collections;

namespace SmartImage.Lib.Engines.Search.Other;

public sealed class ImgOpsEngine : BaseSearchEngine, IUploadEngine
{

	public ImgOpsEngine() : base("https://imgops.com/") { }

	public override SearchEngineOptions Option => SearchEngineOptions.ImgOps;

	UploadEngineOptions INamedEnumOption<UploadEngineOptions>.Option => UploadEngineOptions.ImgOps;

	public Task<UploadResult> UploadAsync(IUniImage query, CancellationToken ct = default)
	{
		// stopgap
		return UploadFileAsync(query.Value, ct);
	}

	public async Task<UploadResult> UploadFileAsync(string file, CancellationToken ct = default)
	{
		Url redirUrl;

		var res = await Client.Request(Url, "store").WithAutoRedirect(true).OnRedirect(r =>
		{
			//
			r.Redirect.Follow = true;
			redirUrl          = r.Redirect.Url;
		}).PostMultipartAsync(act =>
		{
			//
			act.AddFile("photo", file);
		}, cancellationToken: ct);
		var str = await res.GetStringAsync();
		var ur = await ProcessResponseAsync(res, ct);
		return ur;
	}

	public async Task<UploadResult> ProcessResponseAsync(IFlurlResponse response, CancellationToken ct = default)
	{
		return new UploadResult(default, default);
	}

	public void Verify(IUniImage file)
	{
		throw new NotImplementedException();
	}

	public override void Dispose() { }

}