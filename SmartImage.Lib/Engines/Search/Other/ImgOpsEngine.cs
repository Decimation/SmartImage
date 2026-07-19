using Flurl.Http;
using Flurl.Http.Content;
using SmartImage.Lib.Engines.Search.Base;
using SmartImage.Lib.Engines.Upload;
using SmartImage.Lib.Model;
using System.Collections;
using SmartImage.Lib.Engines.Upload.Base;
using SmartImage.Lib.Images.Alloc;

#pragma warning disable CS0162 // Unreachable code detected

namespace SmartImage.Lib.Engines.Search.Other;

public sealed class ImgOpsEngine : BaseSearchEngine, IUploadEngine
{

	public ImgOpsEngine() : base("https://imgops.com/") { }

	public override SearchEngineOptions Option => SearchEngineOptions.ImgOps;

	UploadEngineOption INamedEnumOption<UploadEngineOption>.Option => UploadEngineOption.ImgOps;

	public async Task<IUploadResult> UploadFileAsync(string file, CancellationToken ct = default)
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

	public async Task<IUploadResult> ProcessResponseAsync(IFlurlResponse response, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public void Verify(IAllocImage file)
	{
		throw new NotImplementedException();
	}

	public override void Dispose() { }

}