// Author: Deci | Project: SmartImage.Lib | Name: ICookiesReceiver.cs
// Date: 2024/06/06 @ 17:06:56

using Flurl.Http;
using Kantan.Net.Web;
using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Results.Data;

public interface ICookiesReceiver
{

	public CookieJar Jar { get; }

	[MNNW(true, nameof(Jar))]
	public bool Loaded => Jar != null && Jar.Count != 0;

	public ValueTask<bool> ApplyCookiesAsync(ICookiesProvider provider, CancellationToken token);


}