// Author: Deci | Project: SmartImage.Lib | Name: ICookieEngine.cs
// Date: 2024/06/06 @ 17:06:56

using Kantan.Net.Web;

namespace SmartImage.Lib.Model;

public interface ICookieEngine
{

	public ValueTask<bool> ApplyCookiesAsync(IEnumerable<IBrowserCookie> cookies = null);

}