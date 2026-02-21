// Author: Deci | Project: SmartImage.Lib | Name: ICookiesReceiver.cs
// Date: 2024/06/06 @ 17:06:56

using Flurl.Http;

namespace SmartImage.Lib.Cookies;

public interface ICookiesReceiver
{

	public CookieJar Jar { get; }

	// public ICookiesSource CookiesSource { get; }

	public ValueTask<bool> ApplyCookiesAsync(ICookiesSource source, CancellationToken token = default);

}