using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable CollectionNeverUpdated.Local

namespace SmartImage.Lib.Engines.Search;

public sealed class RepostSleuthEngine : ClientSearchEngine
{
	public RepostSleuthEngine() : base("https://repostsleuth.com/search?url=",
	                                   "https://api.repostsleuth.com/image") { }

	public override SearchEngineOptions EngineOption => SearchEngineOptions.RepostSleuth;

	public override void Dispose()
	{
		base.Dispose();
	}

	#region Overrides of BaseSearchEngine

	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken? token = null)
	{
		var sr = await base.GetResultAsync(query, token);

		var req = await EndpointUrl.SetQueryParam("filter", "true")
		                           .SetQueryParam("url", query.Value)
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
		public int    hamming_distance      { get; set; }
		public double annoy_distance        { get; set; }
		public double hamming_match_percent { get; set; }
		public int    hash_size             { get; set; }
		public string searched_url          { get; set; }
		public Post   post                  { get; set; }
		public int    title_similarity      { get; set; }
	}

	private class Match
	{
		public int    hamming_distance      { get; set; }
		public double annoy_distance        { get; set; }
		public double hamming_match_percent { get; set; }
		public int    hash_size             { get; set; }
		public string searched_url          { get; set; }
		public Post   post                  { get; set; }
		public int    title_similarity      { get; set; }
	}

	private class Post
	{
		public string post_id    { get; set; }
		public string url        { get; set; }
		public object shortlink  { get; set; }
		public string perma_link { get; set; }
		public string title      { get; set; }
		public string dhash_v    { get; set; }
		public string dhash_h    { get; set; }
		public double created_at { get; set; }
		public string author     { get; set; }
		public string subreddit  { get; set; }
	}

	private class Root
	{
		public object         meme_template   { get; set; }
		public ClosestMatch   closest_match   { get; set; }
		public string         checked_url     { get; set; }
		public object         checked_post    { get; set; }
		public SearchSettings search_settings { get; set; }
		public SearchTimes    search_times    { get; set; }
		public List<Match>    matches         { get; set; }
	}

	private class SearchSettings
	{
		public bool   filter_crossposts         { get; set; }
		public bool   filter_same_author        { get; set; }
		public bool   only_older_matches        { get; set; }
		public bool   filter_removed_matches    { get; set; }
		public bool   filter_dead_matches       { get; set; }
		public int    max_days_old              { get; set; }
		public bool   same_sub                  { get; set; }
		public int    max_matches               { get; set; }
		public object target_title_match        { get; set; }
		public string search_scope              { get; set; }
		public bool   check_title               { get; set; }
		public int    max_depth                 { get; set; }
		public bool   meme_filter               { get; set; }
		public int    target_annoy_distance     { get; set; }
		public int    target_meme_match_percent { get; set; }
		public int    target_match_percent      { get; set; }
	}

	private class SearchTimes
	{
		public double pre_annoy_filter_time      { get; set; }
		public double index_search_time          { get; set; }
		public double meme_filter_time           { get; set; }
		public double meme_detection_time        { get; set; }
		public double set_match_post_time        { get; set; }
		public double remove_duplicate_time      { get; set; }
		public double set_match_hamming          { get; set; }
		public double image_search_api_time      { get; set; }
		public double filter_removed_posts_time  { get; set; }
		public double filter_deleted_posts_time  { get; set; }
		public double set_meme_hash_time         { get; set; }
		public double set_closest_meme_hash_time { get; set; }
		public double distance_filter_time       { get; set; }
		public double get_closest_match_time     { get; set; }
		public double total_search_time          { get; set; }
		public double total_filter_time          { get; set; }
		public double set_title_similarity_time  { get; set; }
	}

	#endregion
}