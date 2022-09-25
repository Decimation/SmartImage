using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
#pragma warning disable CS0649

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable CollectionNeverUpdated.Local

namespace SmartImage.Lib.Engines.Search;

public sealed class RepostSleuthEngine : ClientSearchEngine
{
	public RepostSleuthEngine() : base("https://repostsleuth.com/search?url=",
	                                   "https://api.repostsleuth.com/image")
	{
		Timeout = TimeSpan.FromSeconds(4.5);
	}

	public override SearchEngineOptions EngineOption => SearchEngineOptions.RepostSleuth;

	public override void Dispose()
	{
		base.Dispose();
	}

	#region Overrides of BaseSearchEngine

	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken? token = null)
	{
		var sr = await base.GetResultAsync(query, token);

		var req = await EndpointUrl.WithClient(Client)
		                           .SetQueryParam("filter", "true")
		                           .SetQueryParam("url", query.Upload)
		                           .SetQueryParam("same_sub", "false")
		                           .SetQueryParam("filter_author", "true")
		                           .SetQueryParam("only_older", "false")
		                           .SetQueryParam("include_crossposts", "false")
		                           .SetQueryParam("meme_filter", "false")
		                           .SetQueryParam("target_match_percent", "90")
		                           .SetQueryParam("filter_dead_matches", "false")
		                           .SetQueryParam("target_days_old", "0")
		                           .GetAsync();

		var obj = await req.GetJsonAsync<Root>();

		foreach (Match m in obj.matches) {
			var sri = new SearchResultItem(sr)
			{
				Similarity = m.hamming_match_percent,
				Artist     = m.post.author,
				Site       = m.post.subreddit,
				Url        = m.post.url,
				Title      = m.post.title,
				Time       = DateTimeOffset.FromUnixTimeSeconds((long) m.post.created_at).LocalDateTime
			};

			sr.Results.Add(sri);
		}

		return sr;
	}

	protected override async Task<Url> GetRawUrlAsync(SearchQuery query)
	{
		return await base.GetRawUrlAsync(query);
	}

	#endregion

	#region Objects
	
	private class ClosestMatch
	{
		public int hamming_distance;
		public double annoy_distance;
		public double hamming_match_percent;
		public int hash_size;
		public string searched_url;
		public Post post;
		public int title_similarity;
	}

	private class Match
	{
		public int hamming_distance;
		public double annoy_distance;
		public double hamming_match_percent;
		public int hash_size;
		public string searched_url;
		public Post post;
		public int title_similarity;
	}

	private class Post
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

	private class Root
	{
		public object meme_template;
		public ClosestMatch closest_match;
		public string checked_url;
		public object checked_post;
		public SearchSettings search_settings;
		public SearchTimes search_times;
		public List<Match> matches;
	}

	private class SearchSettings
	{
		public bool filter_crossposts;
		public bool filter_same_author;
		public bool only_older_matches;
		public bool filter_removed_matches;
		public bool filter_dead_matches;
		public int max_days_old;
		public bool same_sub;
		public int max_matches;
		public object target_title_match;
		public string search_scope;
		public bool check_title;
		public int max_depth;
		public bool meme_filter;
		public int target_annoy_distance;
		public int target_meme_match_percent;
		public int target_match_percent;
	}

	private class SearchTimes
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