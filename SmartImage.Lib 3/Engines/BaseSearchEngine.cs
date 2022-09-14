global using Url = Flurl.Url;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Flurl.Http;
using JetBrains.Annotations;
using Novus.Utilities;

namespace SmartImage.Lib.Engines;

public abstract class BaseSearchEngine : IDisposable
{
	public abstract SearchEngineOptions EngineOption { get; }

	public virtual string Name => EngineOption.ToString();

	public virtual string BaseUrl { get; }

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

		Trace.WriteLine($"Configured HTTP", nameof(BaseSearchEngine));
	}

	public virtual async Task<SearchResult> GetResultAsync(SearchQuery query)
	{
		var res = new SearchResult(this)
		{
			RawUrl = await GetRawUrlAsync(query)
		};

		return res;
	}

	protected virtual Task<Url> GetRawUrlAsync(SearchQuery query)
	{
		//
		return Task.FromResult<Url>((BaseUrl + query.Upload));
	}

	#region Implementation of IDisposable

	public abstract void Dispose();

	#endregion

	public static readonly BaseSearchEngine[] All =
		ReflectionHelper.CreateAllInAssembly<BaseSearchEngine>(TypeProperties.Subclass).ToArray();
}