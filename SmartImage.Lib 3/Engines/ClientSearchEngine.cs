using Flurl.Http;

namespace SmartImage.Lib.Engines;

public abstract class ClientSearchEngine : BaseSearchEngine
{
	public virtual string EndpointUrl { get; }

	protected FlurlClient Client { get; }

	protected ClientSearchEngine(string baseUrl, string endpoint) : base(baseUrl)
	{
		EndpointUrl = endpoint;

		Client = new FlurlClient(endpoint);
	}

	#region Overrides of BaseSearchEngine

	public override void Dispose()
	{
		Client.Dispose();
	}

	#endregion
}