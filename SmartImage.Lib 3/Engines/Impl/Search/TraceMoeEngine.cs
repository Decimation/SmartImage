using System.Collections;
using System.Diagnostics;
using Flurl.Http;
using JetBrains.Annotations;
using Kantan.Collections;
using Kantan.Text;
using Newtonsoft.Json;
using SmartImage.Lib.Results;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006, IDE0051
namespace SmartImage.Lib.Engines.Impl.Search;

/// <summary>
/// 
/// </summary>
/// <a href="https://soruly.github.io/trace.moe/#/">Documentation</a>
public sealed class TraceMoeEngine : BaseSearchEngine, IClientSearchEngine
{
	public TraceMoeEngine() : base("https://trace.moe/?url=") { }

	public string EndpointUrl => "https://api.trace.moe";

	/// <summary>
	/// Used to retrieve more information about results
	/// </summary>
	private readonly AnilistClient m_anilistClient = new();

	public override string Name => "trace.moe";

	public override SearchEngineOptions EngineOption => SearchEngineOptions.TraceMoe;
	
	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken token = default)
	{

		// https://soruly.github.io/trace.moe/#/

		TraceMoeRootObject tm = null;

		var r = await base.GetResultAsync(query, token);

		try {
			IFlurlRequest request = (EndpointUrl + "/search")
				.AllowAnyHttpStatus()
				.SetQueryParam("url", query.Upload, true);

			var json = await request.GetStringAsync(cancellationToken: token);

			var settings = new JsonSerializerSettings
			{
				Error = (sender, args) =>
				{
					if (Equals(args.ErrorContext.Member, nameof(TraceMoeDoc.episode)) /*&&
						args.ErrorContext.OriginalObject.GetType() == typeof(TraceMoeRootObject)*/) {
						args.ErrorContext.Handled = true;
					}

					Debug.WriteLine($"{Name} :: {args.ErrorContext}", nameof(GetResultAsync));
				}
			};

			tm = JsonConvert.DeserializeObject<TraceMoeRootObject>(json, settings);
		}
		catch (Exception e) {
			Debug.WriteLine($"{Name} :: {nameof(Process)}: {e.Message}", nameof(GetResultAsync));

			goto ret;
		}

		if (tm != null) {
			if (tm.result != null) {
				// Most similar to least similar

				try {
					var results = await ConvertResults(tm, r);

					r.RawUrl = new Url(BaseUrl + query.Upload);
					r.Results.AddRange(results);
				}
				catch (Exception e) {
					r.ErrorMessage = e.Message;
					r.Status       = SearchResultStatus.Failure;
				}

			}
			else if (tm.error != null) {
				Debug.WriteLine($"{Name} :: API error: {tm.error}", nameof(GetResultAsync));
				r.ErrorMessage = tm.error;

				if (r.ErrorMessage.Contains("Search queue is full")) {
					r.Status = SearchResultStatus.Unavailable;
				}
			}
		}

		ret:
		r.Update();

		return r;
	}

	private async Task<IEnumerable<SearchResultItem>> ConvertResults(TraceMoeRootObject obj, SearchResult sr)
	{
		var results = obj.result;
		var items   = new SearchResultItem[results.Count];

		for (int i = 0; i < items.Length; i++) {
			var doc = results[i];
			var sim = Math.Round(doc.similarity * 100.0f, 2);

			string epStr = doc.EpisodeString;

			var result = new SearchResultItem(sr)
			{
				Similarity = sim,
				// Metadata   = new[] { doc.video, doc.image },
				Title = doc.filename,

				Description = $"Episode #{epStr} @ " +
				              $"[{TimeSpan.FromSeconds(doc.from):g} - {TimeSpan.FromSeconds(doc.to):g}]",
			};

			result.Metadata.video = doc.video;
			result.Metadata.image = doc.image;

			try {
				string anilistUrl = ANILIST_URL + doc.anilist;
				string name       = await m_anilistClient.GetTitleAsync((int) doc.anilist);
				result.Source = name;
				result.Url    = new Url(anilistUrl);
			}
			catch (Exception e) {
				Debug.WriteLine($"{Name} :: {e.Message}", nameof(ConvertResults));
			}

			if (result.Similarity < FILTER_THRESHOLD) {
				/*result.OtherMetadata.Add("Note", $"Result may be inaccurate " +
												 $"({result.Similarity.Value / 100:P} " +
												 $"< {FILTER_THRESHOLD / 100:P})");*/
				//todo

				result.Metadata.Warning = $"Similarity below threshold {FILTER_THRESHOLD:P}";
			}

			items[i] = result;
		}

		return items;

	}

	/// <summary>
	/// https://anilist.co/anime/{id}/
	/// </summary>
	private const string ANILIST_URL = "https://anilist.co/anime/";

	/// <summary>
	/// Threshold at which results become inaccurate
	/// </summary>
	public const double FILTER_THRESHOLD = 87.00;

	public override void Dispose()
	{
		m_anilistClient.Dispose();
	}

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

		public string EpisodeString
		{
			get
			{
				string epStr = episode is { } ? episode is string s ? s : episode.ToString() : string.Empty;

				if (episode is IEnumerable e) {
					var epList = e.CastToList()
						.Select(x => long.Parse(x.ToString() ?? string.Empty));

					epStr = epList.QuickJoin();
				}

				return epStr;
			}
		}
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