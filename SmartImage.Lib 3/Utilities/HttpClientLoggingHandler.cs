// Read Stanton SmartImage.Lib LoggingHandler.cs
// 2023-02-14 @ 12:17 AM

using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace SmartImage.Lib.Utilities;

internal class HttpClientLoggingHandler : DelegatingHandler
{
	public HttpClientLoggingHandler(ILogger l)
	{
		m_logger = l;
	}

	public HttpClientLoggingHandler([NotNull] HttpMessageHandler innerHandler) : base(innerHandler)
	{
	}

	private readonly ILogger m_logger;

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
														   CancellationToken cancellationToken)
	{
		m_logger.LogInformation("Request {Request}", request.RequestUri);

		return base.SendAsync(request, cancellationToken);
	}
}
