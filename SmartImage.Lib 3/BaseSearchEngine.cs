namespace SmartImage_3.Lib;

public abstract class BaseSearchEngine
{
	public abstract SearchEngineOptions EngineOption { get; }

	public virtual string Name => EngineOption.ToString();

	public virtual string BaseUrl { get; }

	public abstract Task<SearchResult> GetResultAsync(SearchQuery query);

	protected BaseSearchEngine(string baseUrl)
	{
		BaseUrl = baseUrl;
	}
}