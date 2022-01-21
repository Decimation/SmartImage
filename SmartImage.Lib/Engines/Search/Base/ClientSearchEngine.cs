using SmartImage.Lib.Searching;

namespace SmartImage.Lib.Engines.Search.Base;

/// <summary>
/// Represents a search engine whose results are returned from an API.
/// </summary>
public abstract class ClientSearchEngine : ProcessedSearchEngine
{
	protected ClientSearchEngine(string baseUrl, string endpointUrl) : base(baseUrl)
	{
		
		EndpointUrl = endpointUrl;
	}

	public abstract override SearchEngineOptions EngineOption { get; }
	

	public abstract override EngineSearchType SearchType { get; }

	protected string EndpointUrl { get; }


	protected override object GetProcessingObject(SearchResult r)
	{
		return r.Origin.Query;
	}

}