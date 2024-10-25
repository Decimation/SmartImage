// Read S SmartImage.Lib Extensions.cs
// 2023-07-23 @ 4:29 PM

using Flurl.Http;
using Kantan.Net.Web;
using SmartImage.Lib.Results;
// ReSharper disable InconsistentNaming

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

	internal static readonly string[] Ext = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif"];

	public static HttpMessageHandler GetMostInnerHandler(this HttpMessageHandler self)
	{
		return self is DelegatingHandler handler
			       ? handler.InnerHandler.GetMostInnerHandler()
			       : self;
	}

	public static IFlurlRequest AddChromeImpersonation(this IFlurlRequest req)
	{
		return req.WithHeaders(new
		{
			sec_ch_ua = "\"Chromium\";v=\"104\", \" Not A;Brand\";v=\"99\", \"Google Chrome\";v=\"104\"",
			sec_ch_ua_mobile = "?0",
			sec_ch_ua_platform = "Windows",
			Upgrade_Insecure_Requests = "1",
			User_Agent =
				"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.0.0 Safari/537.36",
			Accept =
				"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9",
			Sec_Fetch_Site  = "none",
			Sec_Fetch_Mode  = "navigate",
			Sec_Fetch_User  = "?1",
			Sec_Fetch_Dest  = "document",
			Accept_Encoding = "gzip, deflate, br",
			Accept_Language = "en-US,en;q=0.9"
		});
	}

}