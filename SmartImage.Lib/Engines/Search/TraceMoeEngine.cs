using System.Collections;
using System.Diagnostics;
using System.Text.Json;
using Argon;
using Flurl;
using Flurl.Http;
using JetBrains.Annotations;
using Kantan.Collections;
using Kantan.Text;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Clients;
using SmartImage.Lib.Engines.Results;
using SmartImage.Lib.Engines.Results.Model;
using SmartImage.Lib.Results.Data;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006, IDE0051
namespace SmartImage.Lib.Engines.Search;

/// <summary>
/// 
/// </summary>
/// <a href="https://soruly.github.io/trace.moe/#/">Documentation</a>
public sealed class TraceMoeEngine : BaseSearchEngine, IEndpointUrl, IDisposable
{

	public TraceMoeEngine() : base(URL_QUERY)
	{
		Timeout = TimeSpan.FromSeconds(25);
	}

	public Url Endpoint => URL_API;

	/// <summary>
	/// Threshold at which results become inaccurate
	/// </summary>
	public const double FILTER_THRESHOLD = 87.00;

	private const string URL_API   = "https://api.trace.moe";
	private const string URL_QUERY = "https://trace.moe/?url=";

	public override string Name => "trace.moe";

	public override SearchEngineOptions EngineOption => SearchEngineOptions.TraceMoe;
	public override ValueTask<bool> ApplyConfigAsync(SearchConfig cfg, CancellationToken ct = default)
	{
		return ValueTask.FromResult(true);

	}
	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken token = default)
	{

		// https://soruly.github.io/trace.moe/#/

		TraceMoeRootObject tm = null;

		var sr = await base.GetResultAsync(query, token);

		try {
			IFlurlRequest request = Client.Request(Endpoint, "/search")
				.WithTimeout(Timeout)
				.SetQueryParam("url", query.Upload, true);

			using var response = await request.GetAsync(cancellationToken: token);

			tm = await response.GetJsonAsync<TraceMoeRootObject>();
		}
		catch (Exception e) {
			Logger.LogError(e, "{Name} in {Fn}", Name, nameof(GetResultAsync));
			sr.ErrorMessage = e.Message;
			sr.Status       = SearchResultStatus.UnknownError;
			goto ret;
		}

		if (tm != null) {
			if (tm.Result != null) {
				// Most similar to the least similar

				try {
					sr.Results.EnsureCapacity(sr.Results.Count + tm.Result.Count);

					foreach (var doc in tm.Result) {
						var tr = await doc.ToItem(sr);
						sr.Results.Add(tr);
					}

					sr.Status = SearchResultStatus.Success;
					sr.RawUrl = new Url(BaseUrl + query.Upload);
				}
				catch (Exception e) {
					sr.ErrorMessage = e.Message;
					sr.Status       = SearchResultStatus.UnknownError;
				}

			}
			else if (tm.Error != null) {
				// Debug.WriteLine($"{Name} :: API error: {tm.Error}", nameof(GetResultAsync));
				Logger.LogDebug("{Name} :: API error {Err} in {Fn}", Name, tm.Error, nameof(GetResultAsync));
				sr.ErrorMessage = tm.Error;
				sr.Status       = SearchResultStatus.IllegalInput;

				if (sr.ErrorMessage.Contains("Search queue is full")) {
					sr.Status = SearchResultStatus.Unavailable;
				}
			}
		}

	ret:
		sr.Update();

		return sr;
	}

	public Task<TraceMoeQuotaObject> GetQuotaAsync()
	{
		return Client.Request(Endpoint, "me")
			.GetJsonAsync<TraceMoeQuotaObject>();
	}

	public override void Dispose() { }

}

#region API Objects

[USI(ImplicitUseTargetFlags.WithMembers)]
public record TraceMoeRootObject : SearchResultItem
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

[USI(ImplicitUseTargetFlags.WithMembers)]
public class TraceMoeDoc
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

	public string EpisodeString { get; set; }

	public Url AnilistUrl { get; }

	[JsonConstructor]
	public TraceMoeDoc()
	{
		AnilistUrl = Url.Combine(AnilistClient.ANILIST_URL, Anilist.ToString());

		EpisodeString = Episode switch
		{
			not null and string => Episode.ToString(),

			IEnumerable e => e.Cast<object>()
				.Select(x =>
				{
					var s1 = x.ToString();

					if (s1.Contains('|')) {
						s1 = s1.Split('|')[0];
					}

					return long.Parse(s1 ?? string.Empty);
				}).QuickJoin(),

			_ => string.Empty
		};

	}

	public async ValueTask<SearchResultItem> ToItem(SearchResult sr)
	{
		var sim = Math.Round(Similarity * 100.0f, 2);

		string name = await AnilistClient.Instance.GetTitleAsync((int) Anilist);

		var result = new SearchResultItem(sr)
		{
			Similarity  = sim,
			Title       = Filename,
			Source      = name,
			Url         = AnilistUrl,
			Description = $"Episode #{EpisodeString} @ [{TimeSpan.FromSeconds(From):g} - {TimeSpan.FromSeconds(To):g}]",
			Metadata    = this
		};

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