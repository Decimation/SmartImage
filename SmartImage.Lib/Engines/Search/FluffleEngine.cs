using System.Net.Http.Headers;
using System.Net.Mime;
using AngleSharp.Css.Values;
using Argon;
using Flurl.Http;
using Microsoft.Net.Http.Headers;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Engines.Search.Base;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Engines.Search;

public class FluffleEngine : BaseSearchEngine, IDisposable
{

	public override SearchEngineOptions Option => SearchEngineOptions.Fluffle;

	public Url Endpoint => URL_ENDPOINT;

	public const string URL_BASE     = "https://fluffle.xyz/";
	public const string URL_API_BASE = "https://api.fluffle.xyz";
	public const string URL_ENDPOINT = $"{URL_API_BASE}/v1/";
	public const string URL_API_NEW  = $"{URL_API_BASE}/exact-search-by-file";


	// todo: update to new API

	public FluffleEngine() : base(URL_BASE)
	{
		MaxLength = 4_194_304; // MiB
	}


	private async Task<bool> GetLegacyResponseAsync(SearchResult sr, SearchQuery query, CancellationToken ct = default)
	{
		IFlurlResponse response = null;

		if (sr.ResponseStatus == SearchResponseStatus.IllegalInput) {
			// return sr;
			goto ret;
		}

		const int LIM_MAX = 32;

		response = await Client.Request(Endpoint, "search")
		                       .WithTimeout(Timeout)
		                       .WithHeaders(new { User_Agent = $"{R1.Name}/{AppSupport.Version}" })
		                       .OnError(e => { e.ExceptionHandled = true; })
		                       .PostMultipartAsync(c =>
		                       {
			                       string file = query.Source.WriteImageToFile();
			                       c.AddFile("file", file, "file");
			                       c.AddString("includeNsfw", true.ToString());
			                       c.AddString("limit", LIM_MAX.ToString());
		                       }, cancellationToken: ct).ConfigureAwait(false);

		if (response is { ResponseMessage.IsSuccessStatusCode: false }) {
			var er = await response.GetJsonAsync<FluffleErrorCode>().ConfigureAwait(false);

			sr.ErrorMessage   = $"{er.Message}: {er.Code}";
			sr.ResponseStatus = SearchResponseStatus.Unknown;

			// return sr;
			goto ret;
		}

		if (response == null) {
			sr.ResponseStatus = SearchResponseStatus.Unknown;
			goto ret;
		}

		var fr = await response.GetJsonAsync<FluffleResponse>().ConfigureAwait(false);
		sr.Results.EnsureCapacity(sr.Results.Count + fr.Results.Count);
		sr.Results.AddRange(fr.Results.Select(result => result.ToItem(sr)));
		sr.ResponseStatus = SearchResponseStatus.Success;

	ret:
		response?.Dispose();
		return true;
	}

	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken ct = default)
	{

		var sr = await base.GetResultAsync(query, ct);
		var ok = await GetLegacyResponseAsync(sr, query, ct);

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

	public SearchResultItem ToItem(SearchResult sr)
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
		return sri;
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