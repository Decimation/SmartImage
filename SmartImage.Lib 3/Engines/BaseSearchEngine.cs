global using Url = Flurl.Url;
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