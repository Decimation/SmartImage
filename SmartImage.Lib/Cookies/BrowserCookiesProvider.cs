// Author: Deci | Project: SmartImage.Lib | Name: CookiesManager.cs

using System.Data;
using System.Diagnostics;
using System.Runtime.Caching;
using Kantan.Net.Web;

namespace SmartImage.Lib.Cookies;

public class BrowserCookiesProvider : ICookiesProvider
{

	private const string CH_NAME = "cookies";

	public BaseCookieReader Reader { get; }

	public MemoryCache Cache { get; }

	public BrowserCookiesProvider(BaseCookieReader reader)
	{
		Reader = reader;
		Cache  = new MemoryCache($"{nameof(BrowserCookiesProvider)}_Cache");
	}

	public async ValueTask OpenAsync()
	{
		// Opening is idempotent
		await Reader.Connection.OpenAsync();
	}

	public async ValueTask CloseAsync()
	{
		await Reader.Connection.CloseAsync();
	}

	public async ValueTask<IList<IBrowserCookie>> GetOrLoadCookiesAsync(CancellationToken ct = default)
	{
		if (!IsOpen) {
			await OpenAsync();
		}

		/*if (IsClosedOrBroken) {
			throw new InvalidOperationException();
		}*/


		var itemPolicy = new CacheItemPolicy()
		{

			// AbsoluteExpiration = 
		};

		var chi = (IList<IBrowserCookie>) Cache.Get(CH_NAME);

		if (chi == null) {

			var cookies = await Reader.ReadCookiesAsync();
			var addOk   = Cache.Add(CH_NAME, cookies, itemPolicy);

			if (addOk) {
				chi = (IList<IBrowserCookie>) Cache.Get(CH_NAME);
			}

		}
		else {
			Trace.WriteLine($"Found {CH_NAME} in cache");
		}

		return chi;
	}

	public bool IsOpen => Reader.Connection.State is ConnectionState.Open;

	public bool IsOpenOrInUse => Reader.Connection.State is < ConnectionState.Broken and >= ConnectionState.Open;

	public bool IsClosedOrBroken => Reader.Connection.State is ConnectionState.Broken or ConnectionState.Closed;

	public void Dispose()
	{
		Debug.WriteLine($"Disposing {nameof(BrowserCookiesProvider)}");
		Reader.Dispose();
		Cache.Dispose();
	}

}