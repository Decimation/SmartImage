using System.Net.NetworkInformation;
using Flurl.Http;
using JetBrains.Annotations;

namespace SmartImage.Lib.Engines;

public interface IClientSearchEngine : IDisposable
{
	public string EndpointUrl { get; }

	[NotNull]
	public Task<IFlurlResponse> GetEndpointResponse(TimeSpan fs)
	{
		return EndpointUrl.WithTimeout(fs)
			.OnError(rx =>
			{
				rx.ExceptionHandled = true;
			})
			.AllowAnyHttpStatus()
			.WithAutoRedirect(true)
			.GetAsync();

	}
}