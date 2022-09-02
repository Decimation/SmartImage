using System.Collections;
using System.Diagnostics;
using Flurl.Http;
using JetBrains.Annotations;
using Kantan.Collections;
using Kantan.Text;
using Newtonsoft.Json;
using static Kantan.Diagnostics.LogCategories;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006, IDE0051
namespace SmartImage.Lib.Engines.Impl;

/// <summary>
/// 
/// </summary>
/// <a href="https://soruly.github.io/trace.moe/#/">Documentation</a>
public sealed class TraceMoeEngine : ClientSearchEngine
{
	public override void Dispose()
	{
		base.Dispose();

	}

	public TraceMoeEngine() : base("https://trace.moe/?url=", "https://api.trace.moe") { }

	//public override TimeSpan Timeout => TimeSpan.FromSeconds(4);

	/// <summary>
	/// Used to retrieve more information about results
	/// </summary>
	private readonly AnilistClient m_anilistClient = new();

	public override string Name => "trace.moe";

	public override SearchEngineOptions EngineOption => SearchEngineOptions.TraceMoe;

	public override async Task<SearchResult> GetResultAsync(SearchQuery query)
	{
		// https://soruly.github.io/trace.moe/#/

		TraceMoeRootObject tm = null;

		var r = new SearchResult();

		try {
			IFlurlRequest request = (EndpointUrl + "/search")
			                        .AllowAnyHttpStatus()
			                        .SetQueryParam("url", query.Upload, true);

			var json = await request.GetStringAsync();

			var settings = new JsonSerializerSettings
			{
				Error = (sender, args) =>
				{
					if (object.Equals(args.ErrorContext.Member, nameof(TraceMoeDoc.episode)) /*&&
					    args.ErrorContext.OriginalObject.GetType() == typeof(TraceMoeRootObject)*/) {
						args.ErrorContext.Handled = true;
					}

					Debug.WriteLine($"{args.ErrorContext}", Name);
				}
			};

			tm = JsonConvert.DeserializeObject<TraceMoeRootObject>(json, settings);
		}
		catch (Exception e) {
			Debug.WriteLine($"{Name}: {nameof(Process)}: {e.Message}");

			goto ret;
		}

		if (tm != null) {
			if (tm.result != null) {
				// Most similar to least similar

				try {
					var results = await ConvertResults(tm, r);

					r.RawUrl        = new Url(BaseUrl + query.Upload);
					r.Results.AddRange(results);
				}
				catch (Exception e) {
					r.ErrorMessage = e.Message;
					r.Status       = SearchResultStatus.Failure;
				}

			}
			else if (tm.error != null) {
				Debug.WriteLine($"{Name}: API error: {tm.error}", C_ERROR);
				r.ErrorMessage = tm.error;

				if (r.ErrorMessage.Contains("Search queue is full")) {
					r.Status = SearchResultStatus.Unavailable;
				}
			}
		}

		ret:

		return r;
	}

	private async Task<IEnumerable<SearchResultItem>> ConvertResults(TraceMoeRootObject obj, SearchResult sr)
	{
		var results      = obj.result;
		var imageResults = new SearchResultItem[results.Count];

		for (int i = 0; i < imageResults.Length; i++) {
			var   doc = results[i];
			float sim = MathF.Round((float) (doc.similarity * 100.0f), 2);

			string epStr = GetEpisodeString(doc);

			var result = new SearchResultItem(sr)
			{
				Similarity  = sim,
				Description = $"Episode #{epStr} @ {TimeSpan.FromSeconds(doc.from)}"
			};

			try {
				string anilistUrl = ANILIST_URL + doc.anilist;
				string name       = await m_anilistClient.GetTitle((int) doc.anilist);
				result.Source = name;
				result.Url    = new Uri(anilistUrl);
			}
			catch (Exception e) {
				Debug.WriteLine($"{e.Message}");
			}

			if (result.Similarity < FILTER_THRESHOLD) {
				/*result.OtherMetadata.Add("Note", $"Result may be inaccurate " +
				                                 $"({result.Similarity.Value / 100:P} " +
				                                 $"< {FILTER_THRESHOLD / 100:P})");*/
				//todo

			}

			imageResults[i] = result;
		}

		return imageResults;

		static string GetEpisodeString(TraceMoeDoc doc)
		{
			object episode = doc.episode;

			string epStr = episode is { } ? episode is string s ? s : episode.ToString() : String.Empty;

			if (episode is IEnumerable e) {
				var epList = e.CastToList()
				              .Select(x => Int64.Parse(x.ToString() ?? String.Empty));

				epStr = epList.QuickJoin();
			}

			return epStr;
		}
	}

	/// <summary>
	/// https://anilist.co/anime/{id}/
	/// </summary>
	private const string ANILIST_URL = "https://anilist.co/anime/";

	/// <summary>
	/// Threshold at which results become inaccurate
	/// </summary>
	private const float FILTER_THRESHOLD = 87.00F;

	#region API Objects

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	private class TraceMoeDoc
	{
		public double from { get; set; }

		public double to { get; set; }

		public long anilist { get; set; }

		public string filename { get; set; }

		/// <remarks>Episode may be a JSON array (edge case) or a normal integer</remarks>
		public object episode { get; set; }

		public double similarity { get; set; }

		public string video { get; set; }

		public string image { get; set; }
	}

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	private class TraceMoeRootObject
	{
		public long frameCount { get; set; }

		public string error { get; set; }

		public List<TraceMoeDoc> result { get; set; }
	}

	#endregion
}