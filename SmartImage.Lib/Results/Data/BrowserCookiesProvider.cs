// Author: Deci | Project: SmartImage.Lib | Name: CookiesManager.cs

global using MemoryCache = Microsoft.Extensions.Caching.Memory.MemoryCache;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using AngleSharp.Css.Dom;
using Flurl.Http;
using Kantan.Net.Web;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Novus.Utilities;

namespace SmartImage.Lib.Results.Data;

using System.Reflection.PortableExecutable;

// using System.Runtime.Caching;
using Results.Data;

public class CookieReaderService : ICookiesService
{

	public BaseCookieReader Reader { get; }

	public MemoryCache Cache { get; }

	public CookieReaderService(BaseCookieReader reader)
	{
		Reader = reader;

		Cache = new MemoryCache(new MemoryCacheOptions()
			                        { });
	}

	

	#region Implementation of IDisposable

	public void Dispose()
	{
		Reader.Dispose();
		Cache.Dispose();
	}

	#endregion

	#region Implementation of ICookiesService

	public async ValueTask<bool> GetOrLoadCookiesAsync(CancellationToken ct = default)
	{
		await Reader.Connection.OpenAsync(ct);

		var cookies = await Reader.ReadCookiesAsync();

		foreach (var cookie in cookies) {
			var entry = CreateEntry(cookie);
		}

		await Reader.Connection.CloseAsync();

		return true;
	}

	#endregion

	#region Implementation of IMemoryCache

	public bool TryGetValue(object key, out object value)
	{
		return Cache.TryGetValue(key, out value);

	}

	public ICacheEntry CreateEntry(object key)
	{
		return Cache.CreateEntry(key);
	}

	public void Remove(object key)
	{

		Cache.Remove(key);
	}

	#endregion

}

