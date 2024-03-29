// Read S SmartImage.Lib Extensions.cs
// 2023-07-23 @ 4:29 PM

using Kantan.Net.Web;
using SmartImage.Lib.Results;

namespace SmartImage.Lib.Utilities;

public static class SearchHelper
{
	public static bool IsSuccessful(this SearchResultStatus s)
	{
		return (!s.IsError() && !s.IsUnknown()) || s is SearchResultStatus.Success;
	}

	public static bool IsUnknown(this SearchResultStatus s)
	{
		return s is SearchResultStatus.NoResults or SearchResultStatus.None;
	}

	public static bool IsError(this SearchResultStatus s)
	{
		return s is SearchResultStatus.Failure or SearchResultStatus.IllegalInput 
			       or SearchResultStatus.Unavailable or SearchResultStatus.Cooldown;
	}

	#region 

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

	#endregion

	internal static readonly string[] Ext = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif"];

}