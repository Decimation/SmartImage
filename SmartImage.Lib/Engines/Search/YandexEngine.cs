// Author: Deci | Project: SmartImage.Lib | Name: YandexEngine.cs
// Date: 2024/06/06 @ 14:06:00

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Flurl.Http;
using Kantan.Net.Utilities;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Engines.Search.Base;

// ReSharper disable SuggestVarOrType_SimpleTypes

#pragma warning disable 8602

namespace SmartImage.Lib.Engines.Search;

public sealed class YandexEngine : BaseSearchEngine
{

	public const string URL_YANDEX = "https://yandex.com/";

	//"https://yandex.com/images/search?rpt=imageview&url="

	public static readonly Url BaseSearchUrl = Url.Combine(URL_YANDEX, "images", "search");

	public override SearchEngineOptions Option => SearchEngineOptions.Yandex;

	public YandexEngine() : base("https://yandex.com/images/search?rpt=imageview&url=")
	{
		Timeout = TimeSpan.FromSeconds(30);
	}

	protected override Url GetRawUrl(SearchQuery query)
	{
		var url = Url.Clone();

		url.QueryParams.AddOrReplace("url", query.Upload);
		url.QueryParams.AddOrReplace("cbir_page", "sites");
		return url;
	}


	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken ct = default)
	{
		var url = GetRawUrl(query);
		var sr  = new SearchResult(this, url) { };

		IDocument      doc;
		IFlurlResponse res = null;

		try {
			res = await Client.Request(sr.RawUrl)
				      .WithAutoRedirect(true)
				      .AllowAnyHttpStatus()
				      .WithTimeout(Timeout)
				      .GetAsync(cancellationToken: ct).ConfigureAwait(false);

			string str = await res.GetStringAsync().ConfigureAwait(false);

			var parser = new HtmlParser();
			doc = await parser.ParseDocumentAsync(str).ConfigureAwait(false);

			var imagesAppNode = doc.Body.SelectSingleNode(Serialization.S_Yandex_Json);
			var json          = imagesAppNode.TryGetAttribute("data-state");

			if (String.IsNullOrWhiteSpace(json)) {
				goto ret;
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

			// var sitesObjDistinct=sitesObj.DistinctBy(x=>x.OriginalImage.Url);

			// sr.Results.AddRange(sitesObj);


		}
		catch (Exception e) {
			// Console.WriteLine(e);
			// throw;
			doc = null;
			Logger.LogError(e, "{Name} error", Name);

			sr.ResponseStatus = SearchResponseStatus.Unknown;
		}
		finally { }


		sr.ResponseStatus = SearchResponseStatus.Success;
	ret:
		sr.Update();
		res?.Dispose();

		// str?.Dispose();
		doc?.Dispose();
		return sr;
	}

	

	public override void Dispose() { }

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
			Metadata = this
		};
	}

}