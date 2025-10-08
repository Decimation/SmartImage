// Author: Deci | Project: SmartImage.Lib | Name: CookiesManager.cs

using System.Collections;
using System.Collections.Frozen;
using System.Data;
using System.Diagnostics;
using System.Runtime.Caching;
using Kantan.Net.Web;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using SmartImage.Lib.Utilities.Integration;

namespace SmartImage.Lib.Cookies;

public class BrowserCookiesSource : ICookiesSource
{

	private const string CH_NAME = "cookies";

	private readonly BaseCookiesDatabaseReader m_reader;

	private IList<ICookie> m_cookies;

	[ICBN]
	public static readonly Lazy<ICookiesSource> Default = new(static () =>
	{
		if (BaseOSIntegration.Integration.IsFirefoxInstalled) {
			var cookieFile = FirefoxCookiesDatabaseReader.FindCookieFile();

			if (cookieFile != null) {
				return new BrowserCookiesSource(new FirefoxCookiesDatabaseReader(cookieFile.FullName));
			}

		}

		return null;
	});

	internal BrowserCookiesSource(BaseCookiesDatabaseReader reader)
	{
		m_reader  = reader;
		m_cookies = null;
	}

	public async ValueTask OpenAsync()
	{
		// Opening is idempotent
		await m_reader.Connection.OpenAsync();
	}

	public async ValueTask CloseAsync()
	{
		await m_reader.Connection.CloseAsync();
	}

	public async ValueTask<IList<ICookie>> GetOrLoadCookiesAsync(CancellationToken ct = default)
	{
		if (m_cookies == null) {
			if (!IsOpen) {
				await OpenAsync();
			}

			var cookies = await m_reader.ReadCookiesAsync();
			m_cookies = cookies.AsReadOnly();
			await CloseAsync();
		}
		else { }

		return m_cookies;
	}

	public bool IsOpen => m_reader.Connection.State is ConnectionState.Open;

	public bool IsOpenOrInUse => m_reader.Connection.State is < ConnectionState.Broken and >= ConnectionState.Open;

	public bool IsClosedOrBroken => m_reader.Connection.State is ConnectionState.Broken or ConnectionState.Closed;

	public void Dispose()
	{
		Debug.WriteLine($"Disposing {nameof(BrowserCookiesSource)}");
		m_reader.Dispose();
		m_cookies.Clear();
	}

}