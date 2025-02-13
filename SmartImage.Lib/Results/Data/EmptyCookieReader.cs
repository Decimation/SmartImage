// Author: Deci | Project: SmartImage.Lib | Name: EmptyCookieReader.cs
// Date: 2025/02/13 @ 03:02:17

using Microsoft.Extensions.Caching.Memory;

namespace SmartImage.Lib.Results.Data;

public class EmptyCookieReader : ICookiesService
{

	#region Implementation of IDisposable

	public void Dispose() { }

	#endregion

	#region Implementation of IMemoryCache

	public bool TryGetValue(object key, out object value)
	{
		value = null;
		return false;
	}

	public ICacheEntry CreateEntry(object key)
	{
		return null;
	}

	public void Remove(object key) { }

	#endregion

	#region Implementation of ICookiesService

	public ValueTask<bool> GetOrLoadCookiesAsync(CancellationToken ct = default)
	{
		return ValueTask.FromResult(false);
	}

	#endregion

}