global using Url = Flurl.Url;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Flurl.Http;
using JetBrains.Annotations;
using Novus.Utilities;

namespace SmartImage.Lib.Engines;

public abstract class BaseSearchEngine : IDisposable
{
	/// <summary>
	/// The corresponding <see cref="SearchEngineOptions"/> of this engine
	/// </summary>
	public abstract SearchEngineOptions EngineOption { get; }

	/// <summary>
	/// Name of this engine
	/// </summary>
	public virtual string Name => EngineOption.ToString();

	public virtual string BaseUrl { get; }

	protected TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(3);

	protected BaseSearchEngine(string baseUrl)
	{
		BaseUrl = baseUrl;
	}

	static BaseSearchEngine()
	{
		/*FlurlHttp.Configure(settings =>
		{
			settings.Redirects.Enabled                    = true; // default true
			settings.Redirects.AllowSecureToInsecure      = true; // default false
			settings.Redirects.ForwardAuthorizationHeader = true; // default false
			settings.Redirects.MaxAutoRedirects           = 15;   // default 10 (consecutive)
		});*/

		// Trace.WriteLine($"Configured HTTP", nameof(BaseSearchEngine));
	}

	public virtual async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken? token = null)
	{
		token ??= CancellationToken.None;

		var res = new SearchResult(this)
		{
			RawUrl = await GetRawUrlAsync(query),
			Status = SearchResultStatus.None
		};

		return res;
	}

	protected virtual Task<Url> GetRawUrlAsync(SearchQuery query)
	{
		//
		return Task.FromResult<Url>((BaseUrl + query.Upload));
	}

	protected void FinalizeResult(SearchResult r)
	{
		if (!r.Results.Any()) {
			r.Status = SearchResultStatus.NoResults;
		}

		if (r.Status != SearchResultStatus.None) {
			r.Status = SearchResultStatus.Success;
		}
	}

	#region Implementation of IDisposable

	public abstract void Dispose();

	#endregion

	public static readonly BaseSearchEngine[] All =
		ReflectionHelper.CreateAllInAssembly<BaseSearchEngine>(TypeProperties.Subclass).ToArray();
}