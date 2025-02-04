// Author: Deci | Project: SmartImage.Lib | Name: DefaultCookiesProvider.cs
// Date: 2025/02/04 @ 12:02:26

using Kantan.Net.Web;

namespace SmartImage.Lib.Results.Data;

public class DefaultCookiesProvider : ICookiesProvider
{

	public List<IBrowserCookie> Cookies { get; }

	public DefaultCookiesProvider()
	{
		Cookies = new List<IBrowserCookie>();
	}

	public ValueTask<IList<IBrowserCookie>> GetOrLoadCookiesAsync(CancellationToken ct = default)
	{
		return ValueTask.FromResult<IList<IBrowserCookie>>(Cookies);
	}

	public void Dispose()
	{
		Cookies.Clear();
	}

}