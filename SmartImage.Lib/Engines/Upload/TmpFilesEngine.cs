using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Flurl.Http;
using Kantan.Numeric;
using Microsoft.Identity.Client;
using SmartImage.Lib.Engines.Upload.Base;
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

		var page = await tmpRes.Data.Url.GetStringAsync(cancellationToken: ct);

		var       parser = new HtmlParser();
		using var doc    = await parser.ParseDocumentAsync(page);
		var       tr     = doc.QuerySelectorAll("tbody > tr > th, td");

		var ur = new TmpFilesUploadResult(tr);
		return ur;
	}

}

#region

public class TmpFilesUploadResult : UploadResult
{
	public string FileName { get; }

	public DateTime Expiration { get; }

	public TmpFilesUploadResult(IHtmlCollection<IElement> elems)
	{
		FileName = elems[1].TextContent;
		var sizeStr = elems[3].TextContent.Split(' ', StringSplitOptions.TrimEntries);

		if (Double.TryParse(sizeStr[0], out var cvVal)) {
			var cvUnit  = sizeStr[1];
			var sizeVal = MathHelper.ParseByteUnit(cvVal, cvUnit);
			Length = (long) sizeVal;
		}

		Url = elems[5].TextContent;
		
		// Final element is UTC specifier
		var dt = elems[7].TextContent;
		Expiration = DateTime.Parse(dt[0..dt.LastIndexOf(' ')]);
	}

}

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