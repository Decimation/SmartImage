using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Flurl.Http;
using SmartImage.Lib.Model;
using SmartImage.Lib.Results;
using JsonSerializer = System.Text.Json.JsonSerializer;

#pragma warning disable CS0649
#pragma warning disable IL2026

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable CollectionNeverUpdated.Local

namespace SmartImage.Lib.Engines.Impl.Search;

public sealed class RepostSleuthEngine : BaseSearchEngine, IDisposable
{

	private const string URL_API   = "https://api.repostsleuth.com/image";
	private const string URL_QUERY = "https://repostsleuth.com/search?url=";

	private static readonly JsonSerializerOptions JsOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
	{
		IncludeFields = true,
	};

	public RepostSleuthEngine() : base(URL_QUERY, URL_API)
	{
		// Timeout = TimeSpan.FromSeconds(4.5);
	}

	public override SearchEngineOptions EngineOption => SearchEngineOptions.RepostSleuth;

	public override void Dispose() { }

	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken token = default)
	{
		var sr = await base.GetResultAsync(query, token);

		RepostSleuthResult obj = null;

		try {
			var s = await Client.Request(EndpointUrl).SetQueryParams(new
			{

				filter               = true,
				url                  = query.Upload,
				same_sub             = false,
				filter_author        = true,
				only_older           = false,
				include_crossposts   = false,
				meme_filter          = false,
				target_match_percent = 90,
				filter_dead_matches  = false,
				target_days_old      = 0
			}).GetStringAsync(cancellationToken: token);

			obj = JsonSerializer.Deserialize<RepostSleuthResult>(s, JsOptions);
		}
		catch (JsonException e) {
			sr.ErrorMessage = e.Message;
			sr.Status       = SearchResultStatus.Failure;
			goto ret;
		}
		catch (FlurlHttpException e) {
			sr.ErrorMessage = e.Message;
			sr.Status       = SearchResultStatus.Unavailable;

			goto ret;
		}

		if (obj?.matches == null || (obj is { matches: not null } && (obj.matches.Any()))) {
			sr.Status = SearchResultStatus.NoResults;
			goto ret;
		}
		
		foreach (var rpm in obj.matches) {
			sr.Results.Add(rpm.Convert(sr, out _));
		}

	ret:
		sr.Update();
		return sr;

	}

	#region API Objects

	private class RepostSleuthClosestMatch
	{

		public int              hamming_distance;
		public double           annoy_distance;
		public double           hamming_match_percent;
		public int              hash_size;
		public string           searched_url;
		public RepostSleuthPost post;
		public int              title_similarity;

	}

	private class RepostSleuthMatch : IResultConvertable
	{

		public int              hamming_distance;
		public double           annoy_distance;
		public double           hamming_match_percent;
		public int              hash_size;
		public string           searched_url;
		public RepostSleuthPost post;
		public double           title_similarity;

		public SearchResultItem Convert(SearchResult sr, out SearchResultItem[] children)
		{
			children = [];

			return new SearchResultItem(sr)
			{
				Similarity = hamming_match_percent,
				Artist     = post.author,
				Site       = post.subreddit,
				Url        = post.url,
				Title      = post.title,
				Time       = DateTimeOffset.FromUnixTimeSeconds((long) post.created_at).LocalDateTime
			};
		}

	}

	private class RepostSleuthPost
	{

		public string post_id;
		public string url;
		public object shortlink;
		public string perma_link;
		public string title;
		public string dhash_v;
		public string dhash_h;
		public double created_at;
		public string author;
		public string subreddit;

	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	private class RepostSleuthResult
	{

		public object                     meme_template;
		public RepostSleuthClosestMatch   closest_match;
		public string                     checked_url;
		public object                     checked_post;
		public RepostSleuthSearchSettings search_settings;
		public RepostSleuthSearchTimes    search_times;
		public List<RepostSleuthMatch>    matches;

	}

	private class RepostSleuthSearchSettings
	{

		public bool   filter_crossposts;
		public bool   filter_same_author;
		public bool   only_older_matches;
		public bool   filter_removed_matches;
		public bool   filter_dead_matches;
		public int    max_days_old;
		public bool   same_sub;
		public int    max_matches;
		public object target_title_match;
		public string search_scope;
		public bool   check_title;
		public int    max_depth;
		public bool   meme_filter;
		public double target_annoy_distance;
		public double target_meme_match_percent;
		public double target_match_percent;

	}

	private class RepostSleuthSearchTimes
	{

		public double pre_annoy_filter_time;
		public double index_search_time;
		public double meme_filter_time;
		public double meme_detection_time;
		public double set_match_post_time;
		public double remove_duplicate_time;
		public double set_match_hamming;
		public double image_search_api_time;
		public double filter_removed_posts_time;
		public double filter_deleted_posts_time;
		public double set_meme_hash_time;
		public double set_closest_meme_hash_time;
		public double distance_filter_time;
		public double get_closest_match_time;
		public double total_search_time;
		public double total_filter_time;
		public double set_title_similarity_time;

	}

	#endregion

}