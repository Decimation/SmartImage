global using Url = Flurl.Url;

namespace SmartImage_3.Lib.Engines;

public abstract class BaseSearchEngine : IDisposable
{
	public abstract SearchEngineOptions EngineOption { get; }

	public virtual string Name => EngineOption.ToString();

	public virtual string BaseUrl { get; }

	public abstract Task<SearchResult> GetResultAsync(SearchQuery query);

	protected BaseSearchEngine(string baseUrl)
	{
		BaseUrl = baseUrl;
	}
	protected virtual Url GetRawUri(SearchQuery query)
	{
		//
		return (BaseUrl + query.Upload);
	}

	#region Implementation of IDisposable

	public abstract void Dispose();

	#endregion
}