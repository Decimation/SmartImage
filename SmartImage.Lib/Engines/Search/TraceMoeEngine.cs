using System.Collections;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl.Http;
using JetBrains.Annotations;
using Kantan.Text;
using Microsoft.Extensions.Logging;
using SmartImage.Lib.Clients;
using SmartImage.Lib.Engines.Results;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006, IDE0051
namespace SmartImage.Lib.Engines.Search;

/// <summary>
/// 
/// </summary>
/// <a href="https://soruly.github.io/trace.moe/#/">Documentation</a>
public sealed class TraceMoeEngine : BaseSearchEngine, IDisposable
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

	public override SearchEngineOptions Option => SearchEngineOptions.TraceMoe;

	public override async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken ct = default)
	{

		// https://soruly.github.io/trace.moe/#/

		TraceMoeRootObject tm = null;

		var sr = await base.GetResultAsync(query, ct).ConfigureAwait(false);

		using var response = await SearchByMultipart(query, ct);
		tm = await response.GetJsonAsync<TraceMoeRootObject>().ConfigureAwait(false);

		if (tm is null) {
			Debugger.Break();
			sr.Status = SearchResultStatus.UnknownError;
			goto ret;
		}

		// Most similar to the least similar

		try {
			sr.Results.EnsureCapacity(sr.Results.Count + tm.Result.Count);

			foreach (var doc in tm.Result) {
				var tr = await doc.ToItem(sr).ConfigureAwait(false);
				sr.Results.Add(tr);
			}

			sr.Status = SearchResultStatus.Success;
			sr.RawUrl = new Url(Url + query.Upload);
		}
		catch (Exception e) {
			sr.ErrorMessage = e.Message;
			sr.Status       = SearchResultStatus.UnknownError;
		}

	ret:

		if (!String.IsNullOrWhiteSpace(tm?.Error)) {
			// Debug.WriteLine($"{Name} :: API error: {tm.Error}", nameof(GetResultAsync));
			Logger.LogDebug("{Name} :: API error {Err} in {Fn}", Name, tm.Error, nameof(GetResultAsync));
			sr.ErrorMessage = tm.Error;
			sr.Status       = SearchResultStatus.IllegalInput;

			if (sr.ErrorMessage.Contains("Search queue is full")) {
				sr.Status = SearchResultStatus.Unavailable;
			}
		}

		sr.Update();

		return sr;
	}

	private Task<IFlurlResponse> SearchByMultipart(SearchQuery query, CancellationToken ct)
	{
		IFlurlRequest req = BuildInitialRequest();

		return req.PostMultipartAsync(ac =>
		{
			if (query.Source.IsFile) {
				ac.AddFile("file", query.Source.Value);
			}
			else {
				ac.AddFile("image", query.Source.GetSource(), "image");
			}
		}, cancellationToken: ct);
	}

	private IFlurlRequest BuildInitialRequest()
	{
		var req = Client.Request(Endpoint, "/search")
			.WithTimeout(Timeout);
		return req;
	}

	private Task<IFlurlResponse> SearchByUpload(SearchQuery query, CancellationToken ct)
	{
		IFlurlRequest request = BuildInitialRequest()
			.SetQueryParam("url", query.Upload.Url, true);

		return request.GetAsync(cancellationToken: ct);
	}

	public Task<TraceMoeQuotaObject> GetQuotaAsync()
	{
		return Client.Request(Endpoint, "me")
			.GetJsonAsync<TraceMoeQuotaObject>();
	}


	public override void Dispose() { }

}

// TODO: refactor serialized objects to support polymorphism

#region API Objects

[USI(ImplicitUseTargetFlags.WithMembers)]
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

	// [JPN("Image")]
	public string Image { get; set; }

	public string EpisodeString { get; set; }

	public Url AnilistUrl { get; }

	[JsonConstructor]
	public TraceMoeDoc(double from, double to, long anilist, string filename, object episode, double similarity, string video, string image)
	{
		From       = from;
		To         = to;
		Anilist    = anilist;
		Filename   = filename;
		Episode    = episode;
		Similarity = similarity;
		Video      = video;
		Image      = image;

		AnilistUrl = Url.Combine(AnilistClient.ANILIST_URL, Anilist.ToString());

		EpisodeString = Episode switch
		{
			not null and string => Episode.ToString(),
			long l              => l.ToString(),
			JsonElement e       => e.ToString(),

			// TODO: wtf is this? For what purpose did I write this
			IEnumerable e => e.Cast<object>().Select(static x =>
			{
				var s1 = x.ToString();

				if (s1.Contains('|')) {
					s1 = s1.Split('|')[0];
				}

				return Int64.Parse(s1 ?? String.Empty);
			}).QuickJoin(),

			_ => String.Empty
		};
	}


	public async ValueTask<SearchResultItem> ToItem(SearchResult sr)
	{
		var sim = Math.Round(Similarity * 100.0f, 2);

		string name = await AnilistClient.Instance.GetTitleAsync(Anilist).ConfigureAwait(false);

		var result = new SearchResultItem(sr)
		{
			Similarity  = sim,
			Title       = Filename,
			Thumbnail   = Image,
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