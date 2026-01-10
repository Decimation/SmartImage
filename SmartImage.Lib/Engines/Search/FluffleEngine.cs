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
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Engines.Search;

public class FluffleEngine : BaseSearchEngine, IDisposable
{
	public override SearchEngineOptions EngineOption => SearchEngineOptions.Fluffle;

	public const string URL_ENDPOINT = "https://api.fluffle.xyz/v1/";
	public const string URL_BASE     = "https://fluffle.xyz/";

	public FluffleEngine() : base(URL_BASE)
	{
		MaxLength = 4_194_304; // MiB

		// Timeout = TimeSpan.FromSeconds(10);
	}


	public Url Endpoint => URL_ENDPOINT;


	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken token = default)
	{

		var sr = await base.GetResultAsync(query, token);

		IFlurlResponse response = null;

		if (sr.Status == SearchResultStatus.IllegalInput) {
			// return sr;
			goto ret;
		}

		var hdr = $"{R1.Name}/{AppSupport.Version} (by {R1.Author} on GitHub)";


		response = await Client.Request(Endpoint, "search")
			           .WithHeaders(new
			           {
				           User_Agent = hdr
			           })
			           .WithTimeout(Timeout)
			           .OnError(e => { e.ExceptionHandled = true; })
			           .PostMultipartAsync(c =>
			           {
				           var file = query.Source.WriteImageToFile();
				           c.AddFile("file", file, "file");
				           c.AddString("includeNsfw", true.ToString());
				           c.AddString("limit", 32.ToString());
			           }, cancellationToken: token).ConfigureAwait(false);

		if (response is { ResponseMessage.IsSuccessStatusCode: false }) {
			var er = await response.GetJsonAsync<FluffleErrorCode>().ConfigureAwait(false);

			sr.ErrorMessage = $"{er.Message}: {er.Code}";
			sr.Status       = SearchResultStatus.UnknownError;

			// return sr;
			goto ret;
		}

		if (response == null) {
			sr.Status = SearchResultStatus.UnknownError;
			goto ret;
		}

		var fr = await response.GetJsonAsync<FluffleResponse>().ConfigureAwait(false);
		sr.Results.EnsureCapacity(sr.Results.Count + fr.Results.Count);
		foreach (FluffleResult result in fr.Results) {
			var item = await result.ToItem(sr);
			sr.Results.Add(item);
		}

		sr.Status = SearchResultStatus.Success;
	ret:
		sr.Update();
		response?.Dispose();
		return sr;
	}

	protected override Url GetRawUrl(SearchQuery query)
	{
		return base.GetRawUrl(query);
	}

	

	public override void Dispose()
	{
		GC.SuppressFinalize(this);
	}

}

#region API Objects

public class FluffleErrorCode
{

	[JPN("code")]
	public string Code { get; set; }

	[JPN("message")]
	public string Message { get; set; }

	[JPN("traceId")]
	public string TraceId { get; set; }

}

public class FluffleResultCredit
{

	[JPN("id")]
	public int Id { get; set; }

	[JPN("name")]
	public string Name { get; set; }

}

public class FluffleResult
{

	[JPN("id")]
	public int Id { get; set; }

	[JPN("score")]
	public double Score { get; set; }

	[JPN("match")]
	public string Match { get; set; }

	[JPN("platform")]
	public string Platform { get; set; }

	[JPN("location")]
	public string Location { get; set; }

	[JPN("isSfw")]
	public bool IsSfw { get; set; }

	[JPN("thumbnail")]
	public FluffleResultThumbnail Thumbnail { get; set; }

	[JPN("credits")]
	public List<FluffleResultCredit> Credits { get; set; }

	public ValueTask<SearchResultItem> ToItem(SearchResult sr)
	{

		var sri = new SearchResultItem(sr)
		{
			Artist     = Credits.FirstOrDefault()?.Name,
			Url        = Location,
			Similarity = Math.Round(Score * 100.0d, 2),
			Metadata   = this,
			Thumbnail  = Thumbnail?.Location,
			Site       = Platform
		};
		return ValueTask.FromResult(sri);
	}

}

public class FluffleResponse
{

	[JPN("id")]
	public string Id { get; set; }

	[JPN("stats")]
	public FluffleResultStats Stats { get; set; }

	[JPN("results")]
	public List<FluffleResult> Results { get; set; }

}

public class FluffleResultStats
{

	[JPN("count")]
	public int Count { get; set; }

	[JPN("elapsedMilliseconds")]
	public int ElapsedMilliseconds { get; set; }

}

public class FluffleResultThumbnail
{

	[JPN("width")]
	public int Width { get; set; }

	[JPN("centerX")]
	public int CenterX { get; set; }

	[JPN("height")]
	public int Height { get; set; }

	[JPN("centerY")]
	public int CenterY { get; set; }

	[JPN("location")]
	public string Location { get; set; }

}

#endregion