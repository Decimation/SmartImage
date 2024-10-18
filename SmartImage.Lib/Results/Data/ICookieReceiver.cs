// Author: Deci | Project: SmartImage.Lib | Name: ICookieReceiver.cs
// Date: 2024/06/06 @ 17:06:56

using System.Data;
using Flurl.Http;
using Kantan.Net.Web;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Results.Data;

public interface ICookieReceiver
{

	public ValueTask<bool> ApplyCookiesAsync(ICookieProvider provider, CancellationToken ct = default);

}

public class SmartCookiesJar
{

	public CookieJar Cookies { get; private set; }

	public static readonly SmartCookiesJar Instance = new SmartCookiesJar();

	public async ValueTask<bool> LoadCookies(Browser b)
	{
		var bcr = GetCookieReaderForBrowser(b);

		if (bcr.Connection.State != ConnectionState.Open) {
			await bcr.OpenAsync();

		}

		var read = await bcr.ReadCookiesAsync();

		foreach (var bc in read) {
			Cookies.AddOrReplace(bc.AsFlurlCookie());
		}

		return true;
	}

	public static BaseCookieReader GetCookieReaderForBrowser(Browser b)
	{
		switch (b) {

			case Browser.Unknown:
				goto case default;

			case Browser.Firefox:
				return new FirefoxCookieReader();

				break;

			case Browser.Chromium:
			case Browser.Safari:
			default:
				return null;
		}
	}

}