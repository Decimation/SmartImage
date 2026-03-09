// Author: Deci | Project: SmartImage.Lib | Name: ICookiesReceiver.cs
// Date: 2024/06/06 @ 17:06:56

using Flurl.Http;

namespace SmartImage.Lib.Cookies;

public interface ICookiesReceiver
{

	public ICookiesSource CookiesSource { get; set; }

	public CookieJar Jar { get; }

	// public ICookiesSource CookiesSource { get; }

}