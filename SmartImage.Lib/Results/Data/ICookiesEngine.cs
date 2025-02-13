// Author: Deci | Project: SmartImage.Lib | Name: ICookiesReceiver.cs
// Date: 2024/06/06 @ 17:06:56

using Flurl.Http;
using Kantan.Net.Web;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Results.Data;

public interface ICookiesEngine
{
	public CookieJar Jar { get; }

	public ICookiesService Provider { get; set; }

	public ValueTask<bool> ApplyCookiesAsync(CancellationToken token = default);
}