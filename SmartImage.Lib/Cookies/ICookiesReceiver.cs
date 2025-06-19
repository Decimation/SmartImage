// Author: Deci | Project: SmartImage.Lib | Name: ICookiesReceiver.cs
// Date: 2024/06/06 @ 17:06:56

using System.Net;
using Flurl.Http;

namespace SmartImage.Lib.Cookies;

public interface ICookiesReceiver
{
	public CookieJar Jar { get; }

	public ValueTask<bool> ApplyCookiesAsync(ICookiesProvider provider, CancellationToken token = default);
}