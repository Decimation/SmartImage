// Author: Deci | Project: SmartImage.Lib | Name: ICookieProvider.cs
// Date: 2024/10/15 @ 12:10:00

using System.Net;
using Flurl.Http;
using Kantan.Net.Web;

namespace SmartImage.Lib.Results.Data;

public interface ICookiesProvider : IDisposable
{

	public ValueTask<IList<IBrowserCookie>> LoadCookiesAsync(CancellationToken ct = default);

}