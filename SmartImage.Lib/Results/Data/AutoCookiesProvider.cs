// Author: Deci | Project: SmartImage.Lib | Name: CookiesManager.cs

using System.Data;
using System.Diagnostics;
using System.Net;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using AngleSharp.Css.Dom;
using Flurl.Http;
using Kantan.Net.Web;

namespace SmartImage.Lib.Results.Data;

using System.Runtime.Caching;

public class AutoCookiesProvider : ICookiesProvider
{

	private const string CH_NAME = "cookies";

	public BaseCookieReader Reader { get; }

	public MemoryCache Cache { get; }

	public AutoCookiesProvider()
		: this(new FirefoxCookieReader())
	{
		// todo
	}

	public AutoCookiesProvider(BaseCookieReader reader)
	{
		Reader = reader;
		Cache  = new MemoryCache($"{nameof(AutoCookiesProvider)}_Cache");
	}

	public async ValueTask OpenAsync()
	{
		await Reader.OpenAsync();
	}

	public async ValueTask<IList<IBrowserCookie>> LoadCookiesAsync(CancellationToken ct = default)
	{
		if (!IsOpen) {
			await OpenAsync();
		}

		if (IsClosedOrBroken) {
			throw new InvalidOperationException();
		}

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

	public bool IsOpen => Reader.Connection.State is < ConnectionState.Broken and >= ConnectionState.Open;

	public bool IsClosedOrBroken => Reader.Connection.State is ConnectionState.Broken or ConnectionState.Closed;

	public void Dispose()
	{
		Reader.Dispose();
		Cache.Dispose();
	}

	public static readonly ICookiesProvider Instance = new AutoCookiesProvider();

}

/*public class AutoCookiesProvider : ICookiesProvider
{

	public BaseCookieReader Reader { get; }

	public AutoCookiesProvider()
		: this(new FirefoxCookieReader())
	{
		//todo
	}

	public AutoCookiesProvider(BaseCookieReader reader)
	{
		Reader = reader;
	}

	public async ValueTask Open()
	{
		await Reader.OpenAsync();
	}


	public async ValueTask<bool> LoadCookiesAsync(ICookiesReceiver rcv, CancellationToken ct = default)
	{
		if (IsClosedOrBroken) {
			throw new InvalidOperationException();
		}

		var cookies = await Reader.ReadCookiesAsync();

		foreach (IBrowserCookie bck in cookies) {
			rcv.Jar.AddOrReplace(bck.AsFlurlCookie());

		}

		return true;
	}

	public bool IsOpen => Reader.Connection.State is < ConnectionState.Broken and >= ConnectionState.Open;

	public bool IsClosedOrBroken => Reader.Connection.State is ConnectionState.Broken or ConnectionState.Closed;

	public void Dispose()
	{
		Reader.Dispose();

		// Reader = null;
	}

	public static readonly ICookiesProvider Instance = new AutoCookiesProvider();

}*/