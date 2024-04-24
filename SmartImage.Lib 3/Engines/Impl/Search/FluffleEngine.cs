using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Flurl.Http;
using JetBrains.Annotations;
using Novus.Streams;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using SmartImage.Lib.Model;
using SmartImage.Lib.Results;

namespace SmartImage.Lib.Engines.Impl.Search;

public class FluffleEngine : BaseSearchEngine, IDisposable
{

	public const string URL_ENDPOINT = "https://api.fluffle.xyz/v1/";
	public const string URL_BASE     = "https://fluffle.xyz/";

	public FluffleEngine() : base(URL_BASE, URL_ENDPOINT)
	{
		MaxSize = 4_194_304; // MiB

		// Timeout = TimeSpan.FromSeconds(10);
	}

	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken token = default)
	{

		var sr = await base.GetResultAsync(query, token);

		if (sr.Status == SearchResultStatus.IllegalInput) {
			return sr;
		}

		var hdr = $"{R1.Name}/{SearchClient.Asm.GetName().Version} (by {R1.Author} on GitHub)";

		var response = await Client.Request(EndpointUrl, "search")
			               .WithHeaders(new
			               {
				               User_Agent = hdr
			               })
			               .WithTimeout(Timeout)
			               .AllowAnyHttpStatus()
			               .OnError(e =>
			               {
				               e.ExceptionHandled = true;
			               })
			               .PostMultipartAsync(c =>
			               {
				               // var tmp = query.WriteToFile();
				               query.Stream.TrySeek();

				               c.AddFile("file", query.Stream, "file");
				               query.Stream.TrySeek();

				               c.AddString("includeNsfw", true.ToString());
				               c.AddString("limit", 32.ToString());

				               // c.AddString("platforms", null)
				               // c.AddString("createLink", false)
			               }, cancellationToken: token);

		if (response is { ResponseMessage: { IsSuccessStatusCode: false } }) {
			var er = await response.GetJsonAsync<FluffleErrorCode>();
			sr.ErrorMessage = $"{er.Message}: {er.Code}";
			return sr;
		}

		var fr = await response.GetJsonAsync<FluffleResponse>();

		foreach (FluffleResult result in fr.Results) {
			var item = result.Convert(sr, out var c);
			sr.Results.Add(item);
		}

		return sr;
	}

	protected override async ValueTask<Url> GetRawUrlAsync(SearchQuery query)
	{
		return await base.GetRawUrlAsync(query);
	}

	public override SearchEngineOptions EngineOption => SearchEngineOptions.Fluffle;

	public override void Dispose() { }

}

#region API Objects

public class FluffleErrorCode
{

	[JsonPropertyName("code")]
	public string Code { get; set; }

	[JsonPropertyName("message")]
	public string Message { get; set; }

	[JsonPropertyName("traceId")]
	public string TraceId { get; set; }

}

public class FluffleResultCredit
{

	[JsonPropertyName("id")]
	public int Id { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; }

}

public class FluffleResult : IResultConvertable
{

	[JsonPropertyName("id")]
	public int Id { get; set; }

	[JsonPropertyName("score")]
	public double Score { get; set; }

	[JsonPropertyName("match")]
	public string Match { get; set; }

	[JsonPropertyName("platform")]
	public string Platform { get; set; }

	[JsonPropertyName("location")]
	public string Location { get; set; }

	[JsonPropertyName("isSfw")]
	public bool IsSfw { get; set; }

	[JsonPropertyName("thumbnail")]
	public FluffleResultThumbnail Thumbnail { get; set; }

	[JsonPropertyName("credits")]
	public List<FluffleResultCredit> Credits { get; set; }

	public SearchResultItem Convert(SearchResult sr, out SearchResultItem[] children)
	{
		children = [];

		var sri = new SearchResultItem(sr)
		{
			Artist     = Credits.FirstOrDefault()?.Name,
			Url        = Location,
			Similarity = Math.Round(Score * 100.0d, 2),
			Metadata   = this,
			Thumbnail  = Thumbnail?.Location,
			Site       = Platform
		};
		return sri;
	}

}

public class FluffleResponse
{

	[JsonPropertyName("id")]
	public string Id { get; set; }

	[JsonPropertyName("stats")]
	public FluffleResultStats Stats { get; set; }

	[JsonPropertyName("results")]
	public List<FluffleResult> Results { get; set; }

}

public class FluffleResultStats
{

	[JsonPropertyName("count")]
	public int Count { get; set; }

	[JsonPropertyName("elapsedMilliseconds")]
	public int ElapsedMilliseconds { get; set; }

}

public class FluffleResultThumbnail
{

	[JsonPropertyName("width")]
	public int Width { get; set; }

	[JsonPropertyName("centerX")]
	public int CenterX { get; set; }

	[JsonPropertyName("height")]
	public int Height { get; set; }

	[JsonPropertyName("centerY")]
	public int CenterY { get; set; }

	[JsonPropertyName("location")]
	public string Location { get; set; }

}

#endregion