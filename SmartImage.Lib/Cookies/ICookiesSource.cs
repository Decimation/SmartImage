// Author: Deci | Project: SmartImage.Lib | Name: ICookieProvider.cs
// Date: 2024/10/15 @ 12:10:00

using Kantan.Net.Web;

namespace SmartImage.Lib.Cookies;

public interface ICookiesSource : IDisposable
{

	public ValueTask<IList<ICookie>> GetOrLoadCookiesAsync(CancellationToken ct = default);

}