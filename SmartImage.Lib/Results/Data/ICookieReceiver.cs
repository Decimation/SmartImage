// Author: Deci | Project: SmartImage.Lib | Name: ICookieReceiver.cs
// Date: 2024/06/06 @ 17:06:56

using SmartImage.Lib.Utilities;

namespace SmartImage.Lib.Results.Data;

public interface ICookieReceiver
{

	public ValueTask<bool> ApplyCookiesAsync(ICookieProvider provider, CancellationToken ct = default);

}