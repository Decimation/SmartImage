global using Url = Flurl.Url;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using Flurl.Http;
using JetBrains.Annotations;
using Novus.Utilities;

namespace SmartImage.Lib.Engines;

public abstract class BaseSearchEngine : IDisposable
{
	public const int NA_SIZE = -1;

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

	protected long MaxSize { get; set; } = NA_SIZE;

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

	protected virtual bool Verify(SearchQuery q)
	{
		if (q.Upload is not {}) {
			return false;
		}

		bool b = q.LoadImage(), b2;

		if (b) {
			b = VerifyImage(q.Image);
		}

		if (MaxSize == NA_SIZE || q.Size == NA_SIZE) {
			b2 = true;
		}

		else b2 = q.Size <= MaxSize;

		return b && b2;
	}

	protected virtual bool VerifyImage(Image i)
	{
		return true;
	}

	public virtual async Task<SearchResult> GetResultAsync(SearchQuery query, CancellationToken? token = null)
	{
		bool b = Verify(query);

		var res = new SearchResult(this)
		{
			RawUrl = await GetRawUrlAsync(query),
			Status = !b ? SearchResultStatus.IllegalInput : SearchResultStatus.None
		};
		
		Debug.WriteLine($"{query} - {res.Status}", nameof(GetResultAsync));

		return res;
	}

	protected virtual Task<Url> GetRawUrlAsync(SearchQuery query)
	{
		//
		Url u = ((BaseUrl + query.Upload));

		return Task.FromResult(u);
	}

	#region Implementation of IDisposable

	public abstract void Dispose();

	#endregion

	public static readonly BaseSearchEngine[] All =
		ReflectionHelper.CreateAllInAssembly<BaseSearchEngine>(TypeProperties.Subclass).ToArray();
}