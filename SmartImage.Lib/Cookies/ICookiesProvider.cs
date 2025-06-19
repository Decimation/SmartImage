// Author: Deci | Project: SmartImage.Lib | Name: ICookieProvider.cs
// Date: 2024/10/15 @ 12:10:00

using Kantan.Net.Web;
using SmartImage.Lib.Utilities.Integration;

namespace SmartImage.Lib.Cookies;

public interface ICookiesProvider : IDisposable
{

	public ValueTask<IList<ICookie>> GetOrLoadCookiesAsync(CancellationToken ct = default);

	public static ICookiesProvider GetProvider()
	{
		ICookiesProvider cp = BrowserCookiesProvider.Default.Value 
		                      ?? new ListCookiesProvider();

		return cp;
	}

}