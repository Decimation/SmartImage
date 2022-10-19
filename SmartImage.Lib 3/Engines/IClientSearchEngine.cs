using Flurl.Http;

namespace SmartImage.Lib.Engines;

public interface IClientSearchEngine : IDisposable
{
	public string EndpointUrl { get; }

	public FlurlClient Client { get; }
}