// Author: Deci | Project: SmartImage.Lib | Name: ICookieProvider.cs
// Date: 2024/10/15 @ 12:10:00

using Kantan.Net.Web;

namespace SmartImage.Lib.Results.Data;

public interface ICookieProvider : IDisposable
{

	public IList<IBrowserCookie> Cookies { get; }

	[MNNW(true, nameof(Cookies))]
	public bool Loaded => Cookies != null && Cookies.Count != 0;

	public ValueTask<bool> LoadCookiesAsync(CancellationToken ct = default);

}