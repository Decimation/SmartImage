// Author: Deci | Project: SmartImage.Lib | Name: YandexEngine.cs
// Date: 2024/06/06 @ 14:06:00

using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Flurl;
using Flurl.Http;
using Kantan.Net.Utilities;
using Kantan.Net.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using SmartImage.Lib.Cookies;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Engines.Search.Base;
using SmartImage.Lib.Model;
using SmartImage.Lib.Utilities.Diagnostics;

// ReSharper disable SuggestVarOrType_SimpleTypes

#pragma warning disable 8602

namespace SmartImage.Lib.Engines.Search;

public sealed class YandexEngine : BaseSearchEngine, ICookiesReceiver, ISearchConfigReceiver
{

	public const string URL_YANDEX    = "https://yandex.com/";
	public const string URL_YANDEX_RU = "https://yandex.ru/";

	//"https://yandex.com/images/search?rpt=imageview&url="

	public ICookiesSource CookiesSource { get; set; }

	public CookieJar Jar { get; }

	public override SearchEngineOptions Option => SearchEngineOptions.Yandex;

	public YandexEngine([CBN] ICookiesSource cookiesSource = null) : base(URL_YANDEX)
	{
		Timeout       = TimeSpan.FromSeconds(30);
		Jar           = new CookieJar();
		CookiesSource = cookiesSource ?? new ListCookiesSource();
	}

	protected override Url GetRawUrl(SearchQuery query)
	{
		var url = Url.Combine(Url, "images", "search").SetQueryParams(new
		{
			rpt       = "imageview",
			url       = query.Upload,
			cbir_page = "search-by-image"
		});

		return url;
	}


	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken ct = default)
	{
		var url = GetRawUrl(query);
		var sr  = new SearchResult(this, url) { };

		IFlurlResponse res = null;
		IDocument      doc = null;

		try {

			var req = Client.Request(sr.RawUrl)
			                .WithAutoRedirect(true)
			                .AllowAnyHttpStatus()
			                .WithCookies(Jar)
			                .WithTimeout(Timeout);

			req.Headers.AddOrReplace(HeaderNames.Accept, Serialization.Yandex_Hdr_Accept);
			req.Headers.AddOrReplace(HeaderNames.AcceptEncoding, Serialization.Yandex_Hdr_AcceptEncoding);
			req.Headers.AddOrReplace(HeaderNames.AcceptLanguage, Serialization.Yandex_Hdr_AcceptLanguage);

			if (query.Source.IsFile) {
				res = await req.PostMultipartAsync(content =>
				{
					//
					content.AddFile("file", query.Source.GetSource(), query.Source.Name);
				}, cancellationToken: ct);
			}
			else {
				res = await req.GetAsync(cancellationToken: ct);
			}

			string str = await res.GetStringAsync().ConfigureAwait(false);

			var parser = new HtmlParser();
			doc = await parser.ParseDocumentAsync(str).ConfigureAwait(false);

			//id="ImagesApp-[^"]*"\s*data-state="({.*?})"\s*data-hydrate-priority=

			var imagesAppNodes = doc.Body.SelectNodes(Serialization.S_Yandex_Json);
			var imagesAppNode  = imagesAppNodes.FirstOrDefault();
			var json           = imagesAppNode.TryGetAttribute("data-state");

			if (String.IsNullOrWhiteSpace(json)) {
				throw new SmartImageException("Could not deserialize");
			}


			var jsonNode = JsonNode.Parse(json);
			var sites    = jsonNode["initialState"]["cbirSites"]["sites"];
			var sitesObj = sites.Deserialize(YandexSiteContext.Default.YandexSiteArray);

			var ocr     = jsonNode["initialState"]["cbirOcr"];
			var ocrText = ocr["hasText"].GetValue<bool>() ? ocr["plainText"] : null;
			sr.Overview = $"OCR: {ocrText}";

			foreach (var site in sitesObj) {
				// site.Root = sr;
				var sri = site.ToItem(sr);

				// var sri = site;
				sr.Results.Add(sri);
			}

		}
		catch (SmartImageException sm) {
			Logger.LogError(sm, "{Name} error", Name);

		}
		catch (Exception e) {
			Logger.LogError(e, "Unhandled {Name} error", Name);

			sr.ResponseStatus = SearchResponseStatus.Unknown;
		}
		finally {
			sr.Update();
			res?.Dispose();
			doc?.Dispose();
		}

		return sr;
	}


	public override void Dispose() { }

	public async ValueTask<bool> ApplyConfigAsync(SearchConfig cfg, CancellationToken ct = default)
	{
		//todo
		CookiesSource = cfg.GetCookiesSource();
		var cs = await CookiesSource.GetOrLoadCookiesAsync(ct);

		foreach (var cookie in cs) {

			if (cookie is FirefoxCookie ff) {
				var illegal = ff.Name.StartsWith('$') || ff.Name.Contains(Environment.NewLine);

				if (illegal) {
					continue;
				}
			}

			var asCookie = cookie.AsCookie();
			var domain   = asCookie.Domain;

			if (domain.StartsWith(".yandex")) {
				var flCk = cookie.AsFlurlCookie(domain.EndsWith(".ru") ? URL_YANDEX_RU : URL_YANDEX);
				Jar.AddOrReplace(flCk);
			}
		}

		return true;
	}

}

[JsonSourceGenerationOptions()]
[JsonSerializable(typeof(YandexSite))]
[JsonSerializable(typeof(YandexSite[]))]
internal partial class YandexSiteContext : JsonSerializerContext { }

public record YandexImage
{

	[JPN("url")]
	public string Url { get; set; }

	[JPN("height")]
	public int Height { get; set; }

	[JPN("width")]
	public int Width { get; set; }

}

public record YandexSite
{

	[JPN("title")]
	public string Title { get; set; }

	[JPN("description")]
	public string Description { get; set; }

	[JPN("url")]
	public string Url { get; set; }

	[JPN("domain")]
	public string Domain { get; set; }

	[JPN("thumb")]
	public YandexImage Thumb { get; set; }

	[JPN("originalImage")]
	public YandexImage OriginalImage { get; set; }

	public SearchResultItem ToItem(SearchResult sr)
	{
		return new SearchResultItem(sr)
		{
			Url         = OriginalImage.Url,
			Height      = OriginalImage.Height,
			Width       = OriginalImage.Width,
			Description = Description,
			Title       = Title,
			Site        = Domain,
			Source      = Url,
			Thumbnail   = Thumb.Url.StartsWith("//") ? "https:" + Thumb.Url : Thumb.Url,
			Metadata    = this
		};
	}

}