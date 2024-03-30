// Author: Deci | Project: SmartImage.Lib | Name: CookiesManager.cs

using System.Diagnostics;
using System.Net;
using Flurl.Http;
using Kantan.Net.Web;

namespace SmartImage.Lib.Utilities;

public static class CookiesManager
{
	// [DebuggerHidden]
	public static async Task<List<IBrowserCookie>> ReadCookiesAsync()
	{
		using var ff = new FirefoxCookieReader();
		await ff.OpenAsync();

		var cookies = await ff.ReadCookiesAsync();

		return cookies;
	}

	public static List<IBrowserCookie> Cookies { get; internal set; }

	public static async Task<bool> LoadCookiesAsync(bool force = false)
	{
		var b = Cookies == null || force;

		if (b) {
			Cookies = await ReadCookiesAsync();
			
		}

		return Cookies != null;
	}

}