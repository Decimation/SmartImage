// Author: Deci | Project: SmartImage.Lib | Name: YandexEngine.cs
// Date: 2024/06/06 @ 14:06:00

using System.Diagnostics;
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
using SmartImage.Lib.Images.Uni;
using SmartImage.Lib.Results;
using SmartImage.Lib.Results.Data;

// ReSharper disable SuggestVarOrType_SimpleTypes

#pragma warning disable 8602

namespace SmartImage.Lib.Engines.Impl.Search;

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

	private static (int? w, int? h) ParseResolution(string resText)
	{
		string[] resFull = resText.Split(Strings.Constants.MUL_SIGN);

		int? w = null, h = null;

		if (resFull.Length == 1 && resFull[0] == resText) {
			const string TIMES_DELIM = "&times;";

			if (resText.Contains(TIMES_DELIM)) {
				resFull = resText.Split(TIMES_DELIM);
			}
		}

		if (resFull.Length == 2) {
			w = Int32.Parse(resFull[0]);
			h = Int32.Parse(resFull[1]);
		}

		return (w, h);
	}

#region Overrides of BaseSearchEngine

	protected override Url GetRawUrl(SearchQuery query)
	{
		var url = BaseUrl.Clone();
		url.QueryParams.AddOrReplace("url", query.Upload);
		url.QueryParams.AddOrReplace("cbir_page", "sites");
		return url;
	}

#endregion


	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken token = default)
	{
		// var sr = await base.GetResultAsync(query, token);

		var url = GetRawUrl(query);

		var sr = new SearchResult(this)
		{
			RawUrl = url
		};

		lock (sr.Results) {
			sr.Results.Add(sr.RawResultItem);
		}

		IDocument doc = null;

		IFlurlResponse res = null;

		Stream str = null;

		try {
			res = await Client.Request(sr.RawUrl)
				      .WithTimeout(Timeout)
				      .GetAsync(cancellationToken: token);

			str = await res.GetStreamAsync();

			var parser = new HtmlParser();
			doc = await parser.ParseDocumentAsync(str);

			var imagesAppNode = doc.Body.SelectSingleNode(Serialization.S_Yandex_Json);
			var json          = imagesAppNode.TryGetAttribute("data-state");

			var jsonNode = JsonNode.Parse(json);
			var sites    = jsonNode["initialState"]["cbirSites"]["sites"];
			var sitesObj = sites.Deserialize<YandexSite[]>();
			var sri      = sitesObj.AsParallel().Select(e => e.ToItem(sr));
			sr.Results.AddRange(sri);


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
	ret:
		sr.Update();
		res?.Dispose();
		str?.Dispose();
		doc?.Dispose();
		return sr;
	}


	public override void Dispose() { }

}

public record YandexImage
{

	[JsonPropertyName("url")]
	public string Url { get; set; }

	[JsonPropertyName("height")]
	public int Height { get; set; }

	[JsonPropertyName("width")]
	public int Width { get; set; }

}

public record YandexSite : ISearchResultItemConvertable
{

	[JsonPropertyName("title")]
	public string Title { get; set; }

	[JsonPropertyName("description")]
	public string Description { get; set; }

	[JsonPropertyName("url")]
	public string Url { get; set; }

	[JsonPropertyName("domain")]
	public string Domain { get; set; }

	[JsonPropertyName("thumb")]
	public YandexImage Thumb { get; set; }

	[JsonPropertyName("originalImage")]
	public YandexImage OriginalImage { get; set; }

	public SearchResultItem ToItem(SearchResult sr)
	{
		return new SearchResultItem(sr)
		{
			Title       = Title,
			Description = Description,
			Url         = OriginalImage.Url,
			Site        = Domain,
			Thumbnail   = Thumb.Url.StartsWith("//") ? "https:" + Thumb.Url : Thumb.Url
		};
	}

}