using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Flurl.Http;
using Kantan.Numeric;
using Microsoft.Identity.Client;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Engines.Upload.Base;
using SmartImage.Lib.Model;
using SmartImage.Lib.Utilities;
using SmartImage.Lib.Utilities.Diagnostics;

namespace SmartImage.Lib.Engines.Upload;

public class TmpFilesEngine : BaseUploadEngine
{

#region

	public const long MIN_EXPIRY_SEC     = 60;
	public const long MAX_EXPIRY_SEC     = 86400;
	public const long DEFAULT_EXPIRY_SEC = 3600;

#endregion

#region

	public const string TMPFILES_URL_BASE = "https://tmpfiles.org";
	public const string TMPFILES_URL_API  = $"{TMPFILES_URL_BASE}/api/v1/upload";

#endregion

	public TmpFilesEngine() : base(TMPFILES_URL_BASE) { }

	public override long? MaxLength => 100_000_000;

	public override UploadEngineOptions Option => UploadEngineOptions.TmpFiles;

	public override async Task<IUploadResult> UploadFileAsync(string file, CancellationToken ct = default)
	{
		using var req = await Client.Request(TMPFILES_URL_API).PostMultipartAsync(act =>
		{
			//
			act.AddFile("file", file);
			act.AddString("expire", DEFAULT_EXPIRY_SEC.ToString());
		}, cancellationToken: ct);

		var prc = await ProcessResponseAsync(req, ct);
		return prc;
	}

	public override async Task<IUploadResult> ProcessResponseAsync(IFlurlResponse response, CancellationToken ct = default)
	{
		var str    = await response.GetStringAsync();
		var tmpRes = JsonSerializer.Deserialize<TmpFilesResponse>(str, SearchUtil.DefaultSerializerOptions);

		var       page   = await tmpRes.Data.Url.GetStringAsync(cancellationToken: ct);
		var       parser = new HtmlParser();
		using var doc    = await parser.ParseDocumentAsync(page);

		return TmpFilesUploadResult.ParseSource(tmpRes, doc);
	}

}

#region

public class TmpFilesUploadResult : UploadResult, IParseableItem<TmpFilesResponse, IDocument, TmpFilesUploadResult>
{

	public string Status { get; }

	public Url PageUrl { get; }

	private TmpFilesUploadResult(TmpFilesResponse res, Url absoluteUrl)
	{
		PageUrl = res.Data.Url;
		Url     = absoluteUrl;
		Status  = res.Status;
	}

	public static TmpFilesUploadResult ParseSource(TmpFilesResponse n, IDocument doc)
	{
		const string DL_ELEM_SELECTOR = ".download";

		var tr = doc.QuerySelector(DL_ELEM_SELECTOR);

		var dlHref = tr.GetAttribute("href");

		if (String.IsNullOrWhiteSpace(dlHref)) {
			dlHref = n.Data.Url;
		}

		return new TmpFilesUploadResult(n, dlHref);
	}

}

public sealed class TmpFilesResponse
{

	public string Status { get; set; }

	public TmpFilesResponseData Data { get; set; }

}

public sealed class TmpFilesResponseData : IUrl
{

	public Url Url { get; set; }

}

#endregion