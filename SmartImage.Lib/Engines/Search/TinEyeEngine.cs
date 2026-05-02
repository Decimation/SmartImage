using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Engines.Search.Base;

namespace SmartImage.Lib.Engines.Search;

public sealed class TinEyeEngine : BaseSearchEngine
{

	public TinEyeEngine() : base("https://www.tineye.com/search?url=")
	{
		MaxLength = 10_000_000;
	}

	private const string API_URL = "https://tineye.com/api/v1/result_json/?sort=score&order=desc";

	public override SearchEngineOptions Option => SearchEngineOptions.TinEye;


	public override bool VerifyQuery(SearchQuery q)
	{

		var ok = base.VerifyQuery(q);

		if (!ok) {
			goto ret;
		}

		if (q.Source.Image.Width >= 10000) {
			ok = false;
			goto ret;
		}

	ret:
		return ok;
	}

	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken ct = default)
	{
		var sr = await base.GetResultAsync(query, ct);

		IFlurlResponse response = null;

		if (sr.ResponseStatus == SearchResponseStatus.IllegalInput) {
			goto ret;
		}

		response = await Client.Request(API_URL).PostMultipartAsync(b =>
		{
			//
			b.AddString("url", query.Upload.Url);
		}, cancellationToken: ct).ConfigureAwait(false);

		TinEyeRoot tinEyeRoot = null;

		try {
			var str = await response.GetStringAsync().ConfigureAwait(false);

			tinEyeRoot = (TinEyeRoot) JsonSerializer.Deserialize(str, typeof(TinEyeRoot), TinEyeContext.Default);

			// tinEyeRoot = await req.GetJsonAsync<TinEyeRoot>();
		}
		catch (Exception e) {
			// Debugger.Break();
			Logger.LogError(e, "{Name}", Name);
			sr.ResponseStatus = SearchResponseStatus.Unknown;
			goto ret;
		}

		if (tinEyeRoot?.Matches == null) {
			sr.ResultsFlags |= SearchResultsFlags.NoResults;

			goto ret;
		}

		foreach (TinEyeMatch match in tinEyeRoot.Matches) {
			var backlinks = match.Backlinks;
			var backlink  = backlinks?[0];


			var resultItem = new SearchResultItem(sr)
			{
				Metadata = match,
				Site     = match.Domain,

				// Thumbnail   = match.Backlinks[0].Url,
				Thumbnail = match.ImageUrl,
				Width     = match.Width,
				Height    = match.Height,
			};

			if (backlink != null) {
				resultItem.Url         = backlink.Backlink;
				resultItem.Description = backlink.ImageName;
				resultItem.Time        = DateTime.Parse(backlink?.CrawlDate);

			}

			if (backlinks is { Count: > 1 }) {
				for (int m = 1; m < backlinks.Count; m++) {
					var bl = backlinks[m];

					var resultItemSister = resultItem with
					{
						Url = bl.Backlink,
						Source = bl.SourceId.GetValueOrDefault().ToString(),
						Title = bl.ImageName,
						Time = DateTime.Parse(bl.CrawlDate)
					};

					sr.Results.Add(resultItemSister);

				}

			}

			sr.Results.Add(resultItem);
		}

		sr.ResponseStatus = SearchResponseStatus.Success;

	ret:
		response?.Dispose();
		return sr;
	}

	protected override Url GetRawUrl(SearchQuery query)
	{
		return base.GetRawUrl(query);
	}

	public override void Dispose()
	{
		// Debug.WriteLine($"Disposing {Name}");
		Logger.LogTrace("Disposing {Name}", Name);
	}

	// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);

}

#region API Objects

public class TinEyeQuery
{

	[JPN("key")]
	public string Key { get; set; }

	[JPN("width")]
	public int Width { get; set; }

	[JPN("height")]
	public int Height { get; set; }

	[JPN("filesize")]
	public int Filesize { get; set; }

	[JPN("hash")]
	public string Hash { get; set; }

}

public class TinEyeRoot
{

	[JPN("page")]
	public int Page { get; set; }

	[JPN("sort_selector")]
	public object SortSelector { get; set; }

	[JPN("limit")]
	public int Limit { get; set; }

	[JPN("domain_name")]
	public string DomainName { get; set; }

	[JPN("no_cache")]
	public bool NoCache { get; set; }

	[JPN("image_server")]
	public string ImageServer { get; set; }

	[JPN("load_query_summary")]
	public bool LoadQuerySummary { get; set; }

	[JPN("show_unavailable_domains")]
	public bool ShowUnavailableDomains { get; set; }

	[JPN("sort")]
	public string Sort { get; set; }

	[JPN("order")]
	public string Order { get; set; }

	[JPN("domain")]
	public string Domain { get; set; }

	[JPN("tags")]
	public string Tags { get; set; }

	[JPN("offset")]
	public int Offset { get; set; }

	[JPN("query_hash")]
	public string QueryHash { get; set; }

	[JPN("start")]
	public int Start { get; set; }

	[JPN("end")]
	public int End { get; set; }

	[JPN("total_pages")]
	public int TotalPages { get; set; }

	[JPN("query")]
	public TinEyeQuery Query { get; set; }

	[JPN("matches")]
	public List<TinEyeMatch> Matches { get; set; }

	[JPN("num_matches")]
	public int NumMatches { get; set; }

	[JPN("num_filtered_matches")]
	public int NumFilteredMatches { get; set; }

	[JPN("num_collection_matches")]
	public int NumCollectionMatches { get; set; }

	[JPN("num_stock_matches")]
	public int NumStockMatches { get; set; }

	[JPN("num_unavailable_matches")]
	public int NumUnavailableMatches { get; set; }

	[JPN("str_num_matches")]
	public string StrNumMatches { get; set; }

	[JPN("str_search_time")]
	public string StrSearchTime { get; set; }

	[JPN("query_source")]
	public string QuerySource { get; set; }

}

[JsonSerializable(typeof(TinEyeRoot))]
public partial class TinEyeContext : JsonSerializerContext { }

public class TinEyeMatch
{

	[JPN("image_url")]
	public string ImageUrl { get; set; }

	[JPN("key")]
	public string Key { get; set; }

	[JPN("transform")]
	public TinEyeTransform Transform { get; set; }

	[JPN("domain")]
	public string Domain { get; set; }

	[JPN("domain_unavailable")]
	public bool DomainUnavailable { get; set; }

	[JPN("score")]
	public double Score { get; set; }

	[JPN("width")]
	public int Width { get; set; }

	[JPN("height")]
	public int Height { get; set; }

	[JPN("size")]
	public int Size { get; set; }

	[JPN("format")]
	public string Format { get; set; }

	[JPN("filesize")]
	public int Filesize { get; set; }

	[JPN("overlay")]
	public string Overlay { get; set; }

	[JPN("matching_features")]
	public int MatchingFeatures { get; set; }

	[JPN("backlinks")]
	public List<TinEyeBacklink> Backlinks { get; set; }

	[JPN("tags")]
	public List<object> Tags { get; set; }

	[JPN("promoted")]
	public bool Promoted { get; set; }

	[JPN("domains")]
	public List<TinEyeDomain> Domains { get; set; }

}

public class TinEyeDomain
{

	[JPN("domain_name")]
	public string DomainName { get; set; }

	[JPN("image_name")]
	public string ImageName { get; set; }

	[JPN("backlinks")]
	public List<TinEyeBacklink> Backlinks { get; set; }

}

public class TinEyeTransform
{

	[JPN("m11")]
	public double M11 { get; set; }

	[JPN("m12")]
	public double M12 { get; set; }

	[JPN("m13")]
	public double M13 { get; set; }

	[JPN("m21")]
	public double M21 { get; set; }

	[JPN("m22")]
	public double M22 { get; set; }

	[JPN("m23")]
	public double M23 { get; set; }

}

public class TinEyeBacklink
{

	[JPN("url")]
	public string Url { get; set; }

	[JPN("backlink")]
	public string Backlink { get; set; }

	[JPN("crawl_date")]
	public string CrawlDate { get; set; }

	[JPN("source_id")]
	public long? SourceId { get; set; }

	[JPN("image_name")]
	public string ImageName { get; set; }

}

#endregion