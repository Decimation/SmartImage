using Flurl.Http;

namespace SmartImage_3.Lib;

public abstract class ClientSearchEngine : BaseSearchEngine
{
	public virtual string EndpointUrl { get; }

	protected FlurlClient Client { get; }

	protected ClientSearchEngine(string baseUrl, string endpoint) : base(baseUrl)
	{
		EndpointUrl = endpoint;

		Client = new FlurlClient(endpoint);
	}
}