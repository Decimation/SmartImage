// Author: Deci | Project: SmartImage.Lib | Name: YandexEngine.cs
// Date: 2024/06/06 @ 14:06:00

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Flurl;
using Flurl.Http;
using Kantan.Net.Utilities;
using Kantan.Text;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Images.Uni;

// ReSharper disable SuggestVarOrType_SimpleTypes

#pragma warning disable 8602

namespace SmartImage.Lib.Engines.Search;

public sealed class YandexEngine : BaseSearchEngine
{

	public const string URL_YANDEX = "https://yandex.com/";

	public override SearchEngineOptions EngineOption => SearchEngineOptions.Yandex;

	protected override string[] ErrorBodyMessages
		=>
		[
			"Please confirm that you and not a robot are sending requests",
			"Изображение не загрузилось, попробуйте загрузить другое."

			// "No matching images found"
		];

	public YandexEngine() : base("https://yandex.com/images/search?rpt=imageview&url=")
	{
		Timeout = TimeSpan.FromSeconds(30);
	}

	protected override Url GetRawUrl(SearchQuery query)
	{
		var url = BaseUrl.Clone();

		url.QueryParams.AddOrReplace("url", query.Upload);
		url.QueryParams.AddOrReplace("cbir_page", "sites");
		return url;
	}


	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken token = default)
	{
		// var sr = await base.GetResultAsync(query, token);

		var url = GetRawUrl(query);
		var sr  = new SearchResult(this, url) { };

		IDocument      doc;
		IFlurlResponse res = null;

		try {
			res = await Client.Request(sr.RawUrl)
				      .WithAutoRedirect(true)
				      .AllowAnyHttpStatus()
				      .WithTimeout(Timeout)
				      .GetAsync(cancellationToken: token).ConfigureAwait(false);

			string str = await res.GetStringAsync().ConfigureAwait(false);

			var parser = new HtmlParser();
			doc = await parser.ParseDocumentAsync(str).ConfigureAwait(false);

			var imagesAppNode = doc.Body.SelectSingleNode(Serialization.S_Yandex_Json);
			var json          = imagesAppNode.TryGetAttribute("data-state");

			var jsonNode = JsonNode.Parse(json);
			var sites    = jsonNode["initialState"]["cbirSites"]["sites"];
			var sitesObj = sites.Deserialize<YandexSite[]>(jsonTypeInfo: YandexSiteCtx.Default.YandexSiteArray);

			foreach (var site in sitesObj) {
				// site.Root = sr;
				var sri = site.ToItem(sr);
				sr.Results.Add(sri);
			}

			// sr.Results.AddRange(sitesObj);


		}
		catch (Exception e) {
			// Console.WriteLine(e);
			// throw;
			doc = null;
			Logger.LogError(e, "{Name} error", Name);

			sr.Status = SearchResultStatus.UnknownError;
		}
		finally { }


		sr.Status = SearchResultStatus.Success;
		sr.Update();
		res?.Dispose();

		// str?.Dispose();
		doc?.Dispose();
		return sr;
	}


	public override ValueTask<bool> ApplyConfigAsync(SearchConfig cfg, CancellationToken ct = default)
	{
		return ValueTask.FromResult(true);
	}

	public override void Dispose() { }

}

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(YandexSite))]
[JsonSerializable(typeof(YandexSite[]))]
internal partial class YandexSiteCtx : JsonSerializerContext { }

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


	/*[JsonConstructor]
	public YandexSite(
		string url,
		string title,
		string description,
		string domain,
		YandexImage thumb,
		YandexImage originalImage) : base(null)
	{

		OriginalImage = originalImage;
		Url           = originalImage.Url;
		Domain        = domain;
		Site          = Domain;
		Thumb         = thumb;
		Thumbnail     = Thumb.Url.StartsWith("//") ? "https:" + Thumb.Url : Thumb.Url;
		/*Url       = OriginalImage.Url,
		Site      = Domain,
		Thumbnail = Thumb.Url.StartsWith("//") ? "https:" + Thumb.Url : Thumb.Url#1#
	}*/


	/*public YandexSite() : base(null)
	{

	}

	private YandexSite(SearchResult r) : base(r) { }*/

	public SearchResultItem ToItem(SearchResult sr)
	{
		return new SearchResultItem(sr)
		{
			Url         = OriginalImage.Url,
			Height      = OriginalImage.Height,
			Width       = OriginalImage.Width,
			Description = Description,
			Site        = Domain,
			Thumbnail   = Thumb.Url.StartsWith("//") ? "https:" + Thumb.Url : Thumb.Url,
		};
	}

}