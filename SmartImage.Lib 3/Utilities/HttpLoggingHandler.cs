// Read Stanton SmartImage.Lib LoggingHandler.cs
// 2023-02-14 @ 12:17 AM

using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace SmartImage.Lib.Utilities;

internal class HttpLoggingHandler : DelegatingHandler
{

	public HttpLoggingHandler(ILogger l)
	{
		m_logger = l;
	}

	public HttpLoggingHandler([NotNull] HttpMessageHandler innerHandler) : base(innerHandler) { }

	private readonly ILogger m_logger;

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
	                                                       CancellationToken cancellationToken)
	{
		m_logger.LogDebug("Request {Request}", request.RequestUri);

		return base.SendAsync(request, cancellationToken);
	}

}