// Author: Deci | Project: SmartImage.Lib | Name: ICookieProvider.cs
// Date: 2024/10/15 @ 12:10:00

using System.Net;
using Flurl.Http;
using Kantan.Net.Web;
using SmartImage.Lib.Utilities.Integration;

namespace SmartImage.Lib.Results.Data;

public interface ICookiesProvider : IDisposable
{

	public ValueTask<IList<IBrowserCookie>> GetOrLoadCookiesAsync(CancellationToken ct = default);


	public static ICookiesProvider GetProvider()
	{
		if (BaseOSIntegration.Integration.IsFirefoxInstalled) {
			var cookieFile = FirefoxCookieReader.FindCookieFile();
			if (cookieFile != null) {
				return new BrowserCookiesProvider(new FirefoxCookieReader(cookieFile.FullName));

			}
		}

		return new DefaultCookiesProvider();
	}

}