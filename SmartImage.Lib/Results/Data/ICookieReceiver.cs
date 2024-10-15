// Author: Deci | Project: SmartImage.Lib | Name: ICookieReceiver.cs
// Date: 2024/06/06 @ 17:06:56

using Kantan.Net.Web;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Results.Data;

public interface ICookieReceiver
{

	public ValueTask<bool> ApplyCookiesAsync(ICookieProvider provider, CancellationToken ct = default);

}

public interface ICookieProvider : IDisposable
{

	public IEnumerable<IBrowserCookie> Cookies { get; }

	[MNNW(true, nameof(Cookies))]
	public bool Loaded => Cookies != null;

	public ValueTask<bool> LoadCookiesAsync(CancellationToken ct = default);

}