namespace SmartImage.Lib.Engines;

public interface IClientSearchEngine : IDisposable
{
	public string EndpointUrl { get; }
}