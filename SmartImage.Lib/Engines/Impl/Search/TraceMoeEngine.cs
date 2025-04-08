using System.Collections;
using System.Diagnostics;
using System.Text.Json;
using Flurl;
using Flurl.Http;
using JetBrains.Annotations;
using Kantan.Collections;
using Kantan.Text;
using SmartImage.Lib.Clients;
using SmartImage.Lib.Results;
using SmartImage.Lib.Results.Data;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006, IDE0051
namespace SmartImage.Lib.Engines.Impl.Search;

/// <summary>
/// 
/// </summary>
/// <a href="https://soruly.github.io/trace.moe/#/">Documentation</a>
public sealed class TraceMoeEngine : BaseSearchEngine, IEndpointEngine, IDisposable
{

	public TraceMoeEngine() : base(URL_QUERY)
	{
		Timeout = TimeSpan.FromSeconds(25);
	}

	public Url EndpointUrl => URL_API;

	/// <summary>
	/// https://anilist.co/anime/{id}/
	/// </summary>
	private const string ANILIST_URL = "https://anilist.co/anime/";

	/// <summary>
	/// Threshold at which results become inaccurate
	/// </summary>
	public const double FILTER_THRESHOLD = 87.00;

	private const string URL_API   = "https://api.trace.moe";
	private const string URL_QUERY = "https://trace.moe/?url=";

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
			IFlurlRequest request = Client.Request(Url.Combine(EndpointUrl, ("/search")))
				.WithTimeout(Timeout)
				.SetQueryParam("url", query.Upload, true);

			using var response = await request.GetAsync(cancellationToken: token);

			// var json = await response.GetStringAsync();

			/*
			var settings = new JsonSerializerOptions()
			{
				Error = (sender, args) =>
				{
					if (Equals(args.ErrorContext.Member, nameof(TraceMoeDoc.episode)) /*&&
						args.ErrorContext.OriginalObject.GetType() == typeof(TraceMoeRootObject)#1#) {
						args.ErrorContext.Handled = true;
					}

					Debug.WriteLine($"{Name} :: {args.ErrorContext}", nameof(GetResultAsync));
				}
			};
			*/
			tm = await response.GetJsonAsync<TraceMoeRootObject>();
			// tm = JsonSerializer.Deserialize<TraceMoeRootObject>(json);
		}
		catch (Exception e) {
			Debug.WriteLine($"{Name} :: {nameof(Process)}: {e.Message}", nameof(GetResultAsync));
			r.ErrorMessage = e.Message;
			r.Status       = SearchResultStatus.UnknownError;
			goto ret;
		}

		if (tm != null) {
			if (tm.Result != null) {
				// Most similar to least similar

				try {
					var results = await ConvertResultsAsync(tm, r);
					r.Status = SearchResultStatus.Success;
					r.RawUrl = new Url(BaseUrl + query.Upload);
					r.Results.AddRange(results);
				}
				catch (Exception e) {
					r.ErrorMessage = e.Message;
					r.Status       = SearchResultStatus.UnknownError;
				}

			}
			else if (tm.Error != null) {
				Debug.WriteLine($"{Name} :: API error: {tm.Error}", nameof(GetResultAsync));
				r.ErrorMessage = tm.Error;
				r.Status       = SearchResultStatus.IllegalInput;

				if (r.ErrorMessage.Contains("Search queue is full")) {
					r.Status = SearchResultStatus.Unavailable;
				}
			}
		}

	ret:
		r.Update();

		return r;
	}

	private async Task<IEnumerable<SearchResultItem>> ConvertResultsAsync(TraceMoeRootObject obj, SearchResult sr)
	{
		var results = obj.Result;
		var items   = new SearchResultItem[results.Count];

		for (int i = 0; i < items.Length; i++) {
			var doc    = results[i];
			var result = doc.Convert(sr);

			try {
				string anilistUrl = Url.Combine(ANILIST_URL, doc.Anilist.ToString());
				string name       = await m_anilistClient.GetTitleAsync((int) doc.Anilist);
				result.Source   = name;
				result.Url      = new Url(anilistUrl);
				result.Metadata = doc;
			}
			catch (Exception e) {
				Debug.WriteLine($"{this} :: {e.Message}", nameof(ConvertResultsAsync));
			}

			items[i] = result;
		}

		return items;

	}

	public override void Dispose()
	{
		m_anilistClient.Dispose();
	}

	public Task<TraceMoeQuotaObject> GetQuotaAsync()
	{
		return Client.Request(EndpointUrl,"me")
			.GetJsonAsync<TraceMoeQuotaObject>();
	}

}

#region API Objects

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class TraceMoeRootObject
{

	public long FrameCount { get; set; }

	public string Error { get; set; }

	public List<TraceMoeDoc> Result { get; set; }

}

public class TraceMoeQuotaObject
{

	public string Id { get; set; }

	public long Priority { get; set; }

	public long Concurrency { get; set; }

	public long Quota { get; set; }

	public long QuotaUsed { get; set; }

}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class TraceMoeDoc : IResultConvertable
{

	public double From { get; set; }

	public double To { get; set; }

	public long Anilist { get; set; }

	public string Filename { get; set; }

	/// <remarks>Episode may be a JSON array (edge case) or a normal integer</remarks>
	public object Episode { get; set; }

	public double Similarity { get; set; }

	public string Video { get; set; }

	public string Image { get; set; }

	public string EpisodeString
	{
		get
		{
			string epStr = Episode is { } ? Episode is string s ? s : Episode.ToString() : string.Empty;

			if (Episode is IEnumerable e && e is not string) {
				var epList = e.Cast<object>()
					.Select(x =>
					{
						var s1 = x.ToString();

						if (s1.Contains('|')) {
							s1 = s1.Split('|')[0];
						}

						return long.Parse(s1 ?? string.Empty);
					});

				epStr = epList.QuickJoin();
			}

			return epStr;
		}
	}

	public SearchResultItem Convert(SearchResult sr)
	{
		var sim = Math.Round(Similarity * 100.0f, 2);

		string epStr = EpisodeString;

		var result = new SearchResultItem(sr)
		{
			Similarity = sim,

			// Metadata   = new[] { doc.video, doc.image },
			Title = Filename,

			Description = $"Episode #{epStr} @ " +
			              $"[{TimeSpan.FromSeconds(From):g} - {TimeSpan.FromSeconds(To):g}]",
		};

		// result.Metadata.video = video;
		// result.Metadata.image = image;

		if (result.Similarity < TraceMoeEngine.FILTER_THRESHOLD) {
			/*result.OtherMetadata.Add("Note", $"Result may be inaccurate " +
												 $"({result.Similarity.Value / 100:P} " +
												 $"< {FILTER_THRESHOLD / 100:P})");*/
			//todo

			// result.Metadata.Warning = $"Similarity below threshold {FILTER_THRESHOLD:P}";
		}

		return result;
	}

}

#endregion