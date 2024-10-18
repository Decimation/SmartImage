// Author: Deci | Project: SmartImage.Lib | Name: ICookieProvider.cs
// Date: 2024/10/15 @ 12:10:00

using System.Net;
using Flurl.Http;
using Kantan.Net.Web;

namespace SmartImage.Lib.Results.Data;

public interface ICookieProvider : IDisposable
{
	public CookieJar Jar {get;}

	[MNNW(true, nameof(Jar))]
	public bool Loaded => Jar != null && Jar.Count != 0;

	public ValueTask<bool> LoadCookiesAsync(Browser b, CancellationToken ct = default);

}