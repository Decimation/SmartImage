// Author: Deci | Project: SmartImage.Lib | Name: CookiesManager.cs

using System.Diagnostics;
using System.Net;
using System.Runtime.Caching;
using Flurl.Http;
using Kantan.Net.Web;

namespace SmartImage.Lib.Utilities;

public class CookiesManager : IDisposable
{
	// TODO

	// [DebuggerHidden]
	private static async Task<List<IBrowserCookie>> ReadCookiesAsync()
	{
		using var ff = new FirefoxCookieReader();
		await ff.OpenAsync();

		var cookies = await ff.ReadCookiesAsync();

		return cookies;
	}

	public List<IBrowserCookie> Cookies { get; private set; }

	[MNNW(true, nameof(Cookies))]
	public bool Loaded => Cookies != null;

	private CookiesManager()
	{
		Cookies = null;
	}

	public async Task<bool> LoadCookiesAsync(bool force = false)
	{
		var b = !Loaded || force;

		if (b) {
			Cookies = await ReadCookiesAsync();

		}

		return Loaded;
	}

	public static readonly CookiesManager Instance = new();

	public void Dispose()
	{
		Cookies.Clear();
		Cookies = null;
	}

}