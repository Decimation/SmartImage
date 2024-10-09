using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl.Http;
using SmartImage.Lib.Results;

namespace SmartImage.Lib.Engines.Impl.Search;

public sealed class TinEyeEngine : BaseSearchEngine
{

	public TinEyeEngine() : base("https://www.tineye.com/search?url=") { }

	private const string API_URL = "https://tineye.com/api/v1/result_json/?sort=score&order=desc";

	public override SearchEngineOptions EngineOption => SearchEngineOptions.TinEye;

	public override void Dispose() { }

	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken token = default)
	{
		var sr = await base.GetResultAsync(query, token);

		var req = await Client.Request(API_URL).PostMultipartAsync(b =>
		{
			b.AddString("url", query.Upload);
		}, cancellationToken: token);

		var tinEyeRoot = await req.GetJsonAsync<TinEyeRoot>();

		foreach (TinEyeMatch match in tinEyeRoot.Matches) {
			var resultItem = new SearchResultItem(sr)
			{
				Metadata    = match,
				Site        = match.Domain,
				Url         = match.Backlinks[0].Backlink,
				Description = match.Backlinks[0].ImageName,

				// Thumbnail   = match.Backlinks[0].Url,
				Thumbnail = match.ImageUrl,
				Width     = match.Width,
				Height    = match.Height,
				Time      = DateTime.Parse(match.Backlinks[0].CrawlDate)
			};

			if (match.Backlinks.Count > 1) {
				for (int m = 1; m < match.Backlinks.Count; m++) {
					var bl = match.Backlinks[m];

					var resultItemSister = resultItem with
					{
						Url = bl.Backlink,

						// Thumbnail = bl.Url, 
						Description = bl.ImageName,
						Time = DateTime.Parse(match.Backlinks[0].CrawlDate),
						Parent = resultItem,
					};

					sr.Results.Add(resultItemSister);

				}

			}

			sr.Results.Add(resultItem);
		}

		return sr;
	}

	protected override Url GetRawUrl(SearchQuery query)
	{
		return base.GetRawUrl(query);
	}

	// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);

}

#region API Objects

public class TinEyeQuery
{

	[JsonPropertyName("key")]
	public string Key { get; set; }

	[JsonPropertyName("width")]
	public int Width { get; set; }

	[JsonPropertyName("height")]
	public int Height { get; set; }

	[JsonPropertyName("filesize")]
	public int Filesize { get; set; }

	[JsonPropertyName("hash")]
	public string Hash { get; set; }

}

public class TinEyeRoot
{

	[JsonPropertyName("page")]
	public int Page { get; set; }

	[JsonPropertyName("sort_selector")]
	public object SortSelector { get; set; }

	[JsonPropertyName("limit")]
	public int Limit { get; set; }

	[JsonPropertyName("domain_name")]
	public string DomainName { get; set; }

	[JsonPropertyName("no_cache")]
	public bool NoCache { get; set; }

	[JsonPropertyName("image_server")]
	public string ImageServer { get; set; }

	[JsonPropertyName("load_query_summary")]
	public bool LoadQuerySummary { get; set; }

	[JsonPropertyName("show_unavailable_domains")]
	public bool ShowUnavailableDomains { get; set; }

	[JsonPropertyName("sort")]
	public string Sort { get; set; }

	[JsonPropertyName("order")]
	public string Order { get; set; }

	[JsonPropertyName("domain")]
	public string Domain { get; set; }

	[JsonPropertyName("tags")]
	public string Tags { get; set; }

	[JsonPropertyName("offset")]
	public int Offset { get; set; }

	[JsonPropertyName("query_hash")]
	public string QueryHash { get; set; }

	[JsonPropertyName("start")]
	public int Start { get; set; }

	[JsonPropertyName("end")]
	public int End { get; set; }

	[JsonPropertyName("total_pages")]
	public int TotalPages { get; set; }

	[JsonPropertyName("query")]
	public TinEyeQuery Query { get; set; }

	[JsonPropertyName("matches")]
	public List<TinEyeMatch> Matches { get; set; }

	[JsonPropertyName("num_matches")]
	public int NumMatches { get; set; }

	[JsonPropertyName("num_filtered_matches")]
	public int NumFilteredMatches { get; set; }

	[JsonPropertyName("num_collection_matches")]
	public int NumCollectionMatches { get; set; }

	[JsonPropertyName("num_stock_matches")]
	public int NumStockMatches { get; set; }

	[JsonPropertyName("num_unavailable_matches")]
	public int NumUnavailableMatches { get; set; }

	[JsonPropertyName("str_num_matches")]
	public string StrNumMatches { get; set; }

	[JsonPropertyName("str_search_time")]
	public string StrSearchTime { get; set; }

	[JsonPropertyName("query_source")]
	public string QuerySource { get; set; }

}

public class TinEyeMatch
{

	[JsonPropertyName("image_url")]
	public string ImageUrl { get; set; }

	[JsonPropertyName("key")]
	public string Key { get; set; }

	[JsonPropertyName("transform")]
	public TinEyeTransform Transform { get; set; }

	[JsonPropertyName("domain")]
	public string Domain { get; set; }

	[JsonPropertyName("domain_unavailable")]
	public bool DomainUnavailable { get; set; }

	[JsonPropertyName("score")]
	public double Score { get; set; }

	[JsonPropertyName("width")]
	public int Width { get; set; }

	[JsonPropertyName("height")]
	public int Height { get; set; }

	[JsonPropertyName("size")]
	public int Size { get; set; }

	[JsonPropertyName("format")]
	public string Format { get; set; }

	[JsonPropertyName("filesize")]
	public int Filesize { get; set; }

	[JsonPropertyName("overlay")]
	public string Overlay { get; set; }

	[JsonPropertyName("matching_features")]
	public int MatchingFeatures { get; set; }

	[JsonPropertyName("backlinks")]
	public List<TinEyeBacklink> Backlinks { get; set; }

	[JsonPropertyName("tags")]
	public List<object> Tags { get; set; }

	[JsonPropertyName("promoted")]
	public bool Promoted { get; set; }

	[JsonPropertyName("domains")]
	public List<TinEyeDomain> Domains { get; set; }

}

public class TinEyeDomain
{

	[JsonPropertyName("domain_name")]
	public string DomainName { get; set; }

	[JsonPropertyName("image_name")]
	public string ImageName { get; set; }

	[JsonPropertyName("backlinks")]
	public List<TinEyeBacklink> Backlinks { get; set; }

}

public class TinEyeTransform
{

	[JsonPropertyName("m11")]
	public double M11 { get; set; }

	[JsonPropertyName("m12")]
	public double M12 { get; set; }

	[JsonPropertyName("m13")]
	public double M13 { get; set; }

	[JsonPropertyName("m21")]
	public double M21 { get; set; }

	[JsonPropertyName("m22")]
	public double M22 { get; set; }

	[JsonPropertyName("m23")]
	public double M23 { get; set; }

}

public class TinEyeBacklink
{

	[JsonPropertyName("url")]
	public string Url { get; set; }

	[JsonPropertyName("backlink")]
	public string Backlink { get; set; }

	[JsonPropertyName("crawl_date")]
	public string CrawlDate { get; set; }

	[JsonPropertyName("source_id")]
	public int SourceId { get; set; }

	[JsonPropertyName("image_name")]
	public string ImageName { get; set; }

}

#endregion